using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LMirman.VespaIO
{
	/// <summary>
	/// Default MonoBehaviour that interacts with the <see cref="DevConsole"/> and manages the canvas state of the UI.
	/// </summary>
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
		private int recentCommandIndex = -1;
		private HistoryInput historyInput;
		private float historyInputTime;
		private string virtualText;
		private string lastAutofillPreview;
		protected readonly HashSet<string> autofillExclusions = new HashSet<string>();

		private void OnEnable()
		{
			DevConsole.console.OutputUpdate += DeveloperConsole_HistoryUpdate;
			recentCommandIndex = -1;
			inputText.onValueChanged.AddListener(InputText_OnValueChanged);
			canvasScaler.scaleFactor = NativeSettings.Config.consoleScale;
		}

		private void OnDisable()
		{
			DevConsole.console.OutputUpdate -= DeveloperConsole_HistoryUpdate;
			inputText.onValueChanged.RemoveListener(InputText_OnValueChanged);
		}

		private void DeveloperConsole_HistoryUpdate()
		{
			historyDirty = true;
		}

		private void InputText_OnValueChanged(string value)
		{
			recentCommandIndex = -1;
			autofillExclusions.Clear();
			virtualText = value;
		}

		private void Start()
		{
			if (NativeSettings.Config.preloadType == DevConsole.PreloadType.ConsoleStart)
			{
				Commands.PreloadLookup();
			}

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
			bool shouldInput = DetermineShouldInput(config);
			bool openKey = shouldInput && GetKeysDown(config.openConsoleKeycodes);
			bool exitKey = shouldInput && DetermineShouldExit(config);

			UpdateTraverseCommandHistory();
			UpdateAutofillPreview();
			UpdateAutofillInput();

			if ((DevConsole.ConsoleActive || !DevConsole.console.Enabled) && exitKey)
			{
				SetConsoleState(false);
			}
			else if (!DevConsole.ConsoleActive && openKey)
			{
				SetConsoleState(true);

				if (config.preloadType == DevConsole.PreloadType.ConsoleOpen)
				{
					Commands.PreloadLookup();
				}
			}

			if (historyDirty)
			{
				history.text = DevConsole.console.GetOutputLog();
				historyDirty = false;
			}
		}

		private bool DetermineShouldInput(ConsoleSettingsConfig config)
		{
			return !config.requireHeldKeyToToggle || GetKeysHeld(config.inputWhileHeldKeycodes);
		}

		private bool DetermineShouldExit(ConsoleSettingsConfig config)
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

		private void UpdateAutofillPreview()
		{
			if (inputText.text == lastAutofillPreview)
			{
				return;
			}

			AutoFillValue foundCommand = DevConsole.console.GetAutoFillValue(virtualText, autofillExclusions);
			if (foundCommand != null)
			{
				autofillPreview.text = foundCommand.newWord;
				autofillPreview.enabled = true;
			}
			else
			{
				autofillPreview.enabled = false;
				autofillPreview.text = string.Empty;
			}

			lastAutofillPreview = inputText.text;
		}

		private void UpdateAutofillInput()
		{
			if (!DevConsole.ConsoleActive || !Input.GetKeyDown(KeyCode.Tab))
			{
				return;
			}

			AutoFillValue autoFillValue = DevConsole.console.GetAutoFillValue(virtualText, autofillExclusions);
			if (autoFillValue != null)
			{
				InsertAutofill(autoFillValue);
				return;
			}

			autofillExclusions.Clear();
			autoFillValue = DevConsole.console.GetAutoFillValue(virtualText, autofillExclusions);
			if (autoFillValue != null)
			{
				InsertAutofill(autoFillValue);
			}
		}

		private void InsertAutofill(AutoFillValue autoFillValue)
		{
			autofillExclusions.Add(autoFillValue.newWord);
			inputText.SetTextWithoutNotify($"{virtualText.Substring(0, autoFillValue.originalWord.wordStartIndex)}{autoFillValue.newWord} ");
			inputText.caretPosition = inputText.text.Length;
		}

		private void UpdateTraverseCommandHistory()
		{
			// Traverse command history on up/down arrow key down.
			if (DevConsole.ConsoleActive && Input.GetKeyDown(KeyCode.UpArrow))
			{
				SetRecentCommandInput(1);
				historyInput = HistoryInput.Up;
				historyInputTime = 0;
			}
			else if (DevConsole.ConsoleActive && Input.GetKeyDown(KeyCode.DownArrow))
			{
				SetRecentCommandInput(-1);
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
					SetRecentCommandInput(direction);
					historyInputTime -= 0.15f;
				}

				if (Input.GetKeyUp(stopKeyCode) || recentCommandIndex == -1 || recentCommandIndex == DevConsole.console.recentInputs.Count - 1)
				{
					historyInput = HistoryInput.None;
					historyInputTime = 0;
				}
			}
		}

		private void SetRecentCommandInput(int direction)
		{
			recentCommandIndex = Mathf.Clamp(recentCommandIndex + direction, -1, DevConsole.console.recentInputs.Count - 1);
			if (recentCommandIndex == -1 || DevConsole.console.recentInputs.Count <= 0)
			{
				virtualText = string.Empty;
				inputText.SetTextWithoutNotify(string.Empty);
			}
			else
			{
				LinkedListNode<string> current = DevConsole.console.recentInputs.First;
				for (int i = 0; i < recentCommandIndex; i++)
				{
					current = current.Next;
				}

				virtualText = current.Value;
				inputText.SetTextWithoutNotify(current.Value);
			}

			inputText.caretPosition = inputText.text.Length;
		}

		private bool GetKeysHeld(KeyCode[] keyCodes)
		{
			for (int i = 0; i < keyCodes.Length; i++)
			{
				if (Input.GetKey(keyCodes[i]))
				{
					return true;
				}
			}

			return false;
		}

		private bool GetKeysDown(KeyCode[] keyCodes)
		{
			for (int i = 0; i < keyCodes.Length; i++)
			{
				if (Input.GetKeyDown(keyCodes[i]))
				{
					return true;
				}
			}

			return false;
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
			virtualText = string.Empty;
			recentCommandIndex = -1;
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

		private void OnSubmit(string submitText)
		{
			if (DevConsole.ConsoleActive && !string.IsNullOrWhiteSpace(submitText))
			{
				inputText.SetTextWithoutNotify(string.Empty);
				virtualText = string.Empty;
				DevConsole.Log("> " + submitText);
				DevConsole.console.RunInput(submitText);
				EventSystem.current.SetSelectedGameObject(inputText.gameObject);
				inputText.OnPointerClick(new PointerEventData(EventSystem.current));
				recentCommandIndex = -1;
				autofillExclusions.Clear();
			}
		}

		private enum HistoryInput
		{
			None, Up, Down
		}
	}
}