using JetBrains.Annotations;
using LMirman.VespaIO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Default MonoBehaviour that interacts with the <see cref="DevConsole"/> and manages the canvas state of the UI.
/// </summary>
[PublicAPI]
public class DevConsoleRunner : MonoBehaviour
{
	[Header("Component References")]
	[SerializeField]
	private Canvas canvas;
	[SerializeField]
	private CanvasScaler canvasScaler;
	[SerializeField]
	private GameObject container;
	[SerializeField]
	private TextMeshProUGUI history;
	[SerializeField]
	private TMP_InputField inputText;
	[SerializeField]
	private TMP_Text autofillPreview;

	private bool historyDirty;
	private bool hasNoEventSystem;
	private GameObject previousSelectable;
	private int recentInputIndex = -1;
	private HistoryInput historyInput;
	private float historyInputTime;
	private AutofillValue autofillPreviewValue;

	private void Awake()
	{
		Application.logMessageReceived += ApplicationOnLogMessageReceived;
	}

	private void OnDestroy()
	{
		Application.logMessageReceived -= ApplicationOnLogMessageReceived;
	}

	private void OnEnable()
	{
		DevConsole.console.OutputUpdate += ConsoleOnOutputUpdate;
		inputText.onValueChanged.AddListener(InputText_OnValueChanged);
		canvasScaler.scaleFactor = NativeSettings.Config.consoleScale;
		recentInputIndex = -1;
	}

	private void OnDisable()
	{
		DevConsole.console.OutputUpdate -= ConsoleOnOutputUpdate;
		inputText.onValueChanged.RemoveListener(InputText_OnValueChanged);
	}

	private void ApplicationOnLogMessageReceived(string condition, string stacktrace, LogType type)
	{
		Console.LogStyling logStyling = Console.LogStyling.Plain;
		switch (type)
		{
			case LogType.Error:
				logStyling = Console.LogStyling.Error;
				break;
			case LogType.Assert:
				logStyling = Console.LogStyling.Assert;
				break;
			case LogType.Warning:
				logStyling = Console.LogStyling.Warning;
				break;
			case LogType.Log:
				logStyling = Console.LogStyling.Info;
				break;
			case LogType.Exception:
				logStyling = Console.LogStyling.Exception;
				break;
		}

		DevConsole.Log(condition, logStyling);
	}

	private void ConsoleOnOutputUpdate()
	{
		historyDirty = true;
	}

	private void InputText_OnValueChanged(string value)
	{
		recentInputIndex = -1;
		DevConsole.console.VirtualText = value;
	}

	private void Start()
	{
		// Destroy the graphic raycaster if the console is never expected to utilize it
		if (NativeSettings.Config.closeConsoleOnLeftClick && TryGetComponent(out GraphicRaycaster raycaster))
		{
			Destroy(raycaster);
		}

		DontDestroyOnLoad(this);
		SetConsoleState(false);
		inputText.onSubmit.AddListener(OnSubmit);
		historyDirty = true;
	}

	private void Update()
	{
		ConsoleSettingsConfig config = NativeSettings.Config;
		bool shouldInput = DetermineShouldInput();
		bool openKey = shouldInput && GetKeysDown(config.openConsoleKeycodes);
		bool exitKey = shouldInput && DetermineShouldExit();

		UpdateTraverseCommandHistory();
		UpdateAutofillPreview();

		// Auto fill
		if (DevConsole.ConsoleActive && Input.GetKeyDown(KeyCode.Tab) && DevConsole.console.ApplyNextAutofill(out string newInputText))
		{
			inputText.SetTextWithoutNotify(newInputText);
			inputText.caretPosition = inputText.text.Length;
		}

		// Change active state
		if ((DevConsole.ConsoleActive || !DevConsole.console.Enabled) && exitKey)
		{
			SetConsoleState(false);
		}
		else if (!DevConsole.ConsoleActive && openKey)
		{
			SetConsoleState(true);
		}

		// Update output
		if (historyDirty)
		{
			history.text = DevConsole.console.GetOutputLog();
			historyDirty = false;
		}

		bool DetermineShouldInput()
		{
			return !config.requireHeldKeyToToggle || GetKeysHeld(config.inputWhileHeldKeycodes);
		}

		bool DetermineShouldExit()
		{
			if (GetKeysDown(config.closeAnyConsoleKeycodes))
			{
				return true;
			}
			else if (GetKeysDown(config.closeEmptyConsoleKeycodes) && inputText.text.Length <= 1)
			{
				return true;
			}
			else if (Input.GetMouseButtonDown(0) && config.closeConsoleOnLeftClick)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		void UpdateTraverseCommandHistory()
		{
			// Traverse command history on up/down arrow key down.
			if (DevConsole.ConsoleActive && Input.GetKeyDown(KeyCode.UpArrow))
			{
				SetRecentInput(1);
				historyInput = HistoryInput.Up;
				historyInputTime = 0;
			}
			else if (DevConsole.ConsoleActive && Input.GetKeyDown(KeyCode.DownArrow))
			{
				SetRecentInput(-1);
				historyInput = HistoryInput.Down;
				historyInputTime = 0;
			}

			// Held key scroll
			if (historyInput == HistoryInput.Up)
			{
				Scroll(1, KeyCode.UpArrow);
			}
			else if (historyInput == HistoryInput.Down)
			{
				Scroll(-1, KeyCode.DownArrow);
			}

			void Scroll(int direction, KeyCode stopKeyCode)
			{
				historyInputTime += Time.unscaledDeltaTime;
				if (historyInputTime > 0.5f)
				{
					SetRecentInput(direction);
					historyInputTime -= 0.15f;
				}

				if (Input.GetKeyUp(stopKeyCode) || recentInputIndex == -1 || recentInputIndex == DevConsole.console.recentInputs.Count - 1)
				{
					historyInput = HistoryInput.None;
					historyInputTime = 0;
				}
			}
		}

		void UpdateAutofillPreview()
		{
			if (autofillPreviewValue == DevConsole.console.NextAutofill)
			{
				return;
			}

			autofillPreviewValue = DevConsole.console.NextAutofill;
			if (autofillPreviewValue != null)
			{
				autofillPreview.text = autofillPreviewValue.markupNewWord;
				autofillPreview.enabled = true;
			}
			else
			{
				autofillPreview.enabled = false;
				autofillPreview.text = string.Empty;
			}
		}
	}

	private void OnSubmit(string submitText)
	{
		if (DevConsole.ConsoleActive && !string.IsNullOrWhiteSpace(submitText))
		{
			inputText.SetTextWithoutNotify(string.Empty);
			DevConsole.console.VirtualText = string.Empty;
			DevConsole.console.RunInput(submitText);
			EventSystem.current.SetSelectedGameObject(inputText.gameObject);
			inputText.OnPointerClick(new PointerEventData(EventSystem.current));
			recentInputIndex = -1;
		}
	}

	public void SetConsoleState(bool value)
	{
		DevConsole.ConsoleActive = value;
		if (container)
		{
			container.SetActive(value);
		}

		canvas.enabled = value;
		inputText.enabled = value;
		inputText.SetTextWithoutNotify(string.Empty);
		DevConsole.console.VirtualText = string.Empty;
		recentInputIndex = -1;
		if (EventSystem.current != null)
		{
			if (value)
			{
				previousSelectable = EventSystem.current.currentSelectedGameObject;
				EventSystem.current.SetSelectedGameObject(inputText.gameObject);
				inputText.OnPointerClick(new PointerEventData(EventSystem.current));
			}
			else if (previousSelectable != null && previousSelectable.activeInHierarchy)
			{
				EventSystem.current.SetSelectedGameObject(previousSelectable);
				previousSelectable = null;
			}

			// Clear no event system warning if present since one is present now.
			if (hasNoEventSystem)
			{
				DevConsole.console.Clear();
				DevConsole.PrintWelcome();
				hasNoEventSystem = false;
			}
		}
#if UNITY_EDITOR
		else
		{
			DevConsole.console.Clear();
			DevConsole.Log("No event system present in scene. The developer console cannot function without this.", Console.LogStyling.Error);
			DevConsole.Log("Add an event system by right clicking in the scene Hierarchy > UI > Event System.");
			hasNoEventSystem = true;
		}
#endif
	}

	private void SetRecentInput(int direction)
	{
		recentInputIndex = Mathf.Clamp(recentInputIndex + direction, -1, DevConsole.console.recentInputs.Count - 1);
		string recentInput = DevConsole.console.GetRecentInputByIndex(recentInputIndex);
		DevConsole.console.VirtualText = recentInput;
		inputText.SetTextWithoutNotify(recentInput);
		inputText.caretPosition = inputText.text.Length;
	}

	#region Input Handling
	private bool GetKeysHeld(KeyCode[] keyCodes)
	{
		foreach (KeyCode keyCode in keyCodes)
		{
			if (Input.GetKey(keyCode))
			{
				return true;
			}
		}

		return false;
	}

	private bool GetKeysDown(KeyCode[] keyCodes)
	{
		foreach (KeyCode keyCode in keyCodes)
		{
			if (Input.GetKeyDown(keyCode))
			{
				return true;
			}
		}

		return false;
	}
	#endregion

	private enum HistoryInput
	{
		None, Up, Down
	}
}