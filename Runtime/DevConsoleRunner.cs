using System;
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
		[SerializeField] private Canvas canvas;
		[SerializeField] private CanvasScaler canvasScaler;
		[SerializeField] private GameObject container;
		[SerializeField] private TextMeshProUGUI history;
		[SerializeField] private TMP_InputField inputText;
		[SerializeField] private TMP_Text autofillPreview;

		private bool historyDirty;
		private bool hasNoEventSystem;
		private GameObject previousSelectable;
		private int recentCommandIndex = -1;
		private HistoryInput historyInput;
		private float historyInputTime = 0;
		private string lastInput;
		private string lastAutofillPreview;

		internal static DevConsoleRunner Instance { get; private set; }
		internal static List<string> recentFillSearch = new List<string>();

		private void OnEnable()
		{
			DevConsole.OutputUpdate += DeveloperConsole_HistoryUpdate;
			recentCommandIndex = -1;
			inputText.onValueChanged.AddListener(InputText_OnValueChanged);
			canvasScaler.scaleFactor = ConsoleSettings.Config.consoleScale;
		}

		private void OnDisable()
		{
			DevConsole.OutputUpdate -= DeveloperConsole_HistoryUpdate;
			inputText.onValueChanged.RemoveListener(InputText_OnValueChanged);
		}

		private void DeveloperConsole_HistoryUpdate()
		{
			historyDirty = true;
		}

		private void InputText_OnValueChanged(string value)
		{
			recentCommandIndex = -1;
			recentFillSearch.Clear();
			lastInput = value;
		}

		private void Start()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}

			if (ConsoleSettings.Config.preloadType == DevConsole.PreloadType.ConsoleStart)
			{
				Commands.PreloadLookup();
			}

			// Destroy the graphic raycaster if the console is never expected to utilize it
			if (ConsoleSettings.Config.closeConsoleOnLeftClick && TryGetComponent(out GraphicRaycaster raycaster))
			{
				Destroy(raycaster);
			}

			Instance = this;
			DontDestroyOnLoad(this);
			SetConsoleState(false);
			inputText.onSubmit.AddListener(OnSubmit);
			historyDirty = true;
		}

		private void Update()
		{
			ConsoleSettingsConfig config = ConsoleSettings.Config;
			bool shouldInput = DetermineShouldInput(config);
			bool openKey = shouldInput && GetKeysDown(config.openConsoleKeycodes);
			bool exitKey = shouldInput && DetermineShouldExit(config);

			UpdateTraverseCommandHistory();
			UpdateAutofillPreview();
			UpdateAutofillInput();

			if ((DevConsole.ConsoleActive || !DevConsole.ConsoleEnabled) && exitKey)
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
				history.text = DevConsole.output.ToString();
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

			Command foundCommand = Commands.FindFirstMatch(inputText.text, recentFillSearch);
			if (string.IsNullOrWhiteSpace(inputText.text))
			{
				autofillPreview.text = "help";
				autofillPreview.enabled = true;
			}
			else if (foundCommand != null)
			{
				autofillPreview.text = foundCommand.Key;
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

			if (string.IsNullOrWhiteSpace(lastInput))
			{
				inputText.SetTextWithoutNotify("help ");
				inputText.caretPosition = inputText.text.Length;
				return;
			}

			// This clears the recent fill list in case the user has tab through all options
			if (Commands.FindFirstMatch(lastInput, recentFillSearch) == null)
			{
				recentFillSearch.Clear();
			}

			Command foundCommand = Commands.FindFirstMatch(lastInput, recentFillSearch);
			if (foundCommand != null)
			{
				recentFillSearch.Add(foundCommand.Key);
				inputText.SetTextWithoutNotify(foundCommand.Key + ' ');
				inputText.caretPosition = inputText.text.Length;
			}
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

				if (Input.GetKeyUp(stopKeyCode) || recentCommandIndex == -1 || recentCommandIndex == DevConsole.recentCommands.Count - 1)
				{
					historyInput = HistoryInput.None;
					historyInputTime = 0;
				}
			}
		}

		private void SetRecentCommandInput(int direction)
		{
			recentCommandIndex = Mathf.Clamp(recentCommandIndex + direction, -1, DevConsole.recentCommands.Count - 1);
			if (recentCommandIndex == -1 || DevConsole.recentCommands.Count <= 0)
			{
				inputText.SetTextWithoutNotify(string.Empty);
			}
			else
			{
				LinkedListNode<string> current = DevConsole.recentCommands.First;
				for (int i = 0; i < recentCommandIndex; i++)
				{
					current = current.Next;
				}
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
					DevConsole.Clear();
					DevConsole.PrintWelcome();
					hasNoEventSystem = false;
				}
			}
#if UNITY_EDITOR
			else
			{
				DevConsole.Clear();
				DevConsole.Log("<color=red>ERROR:</color> No event system present in scene. The developer console cannot function without this.");
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
				DevConsole.Log("> " + submitText);
				DevConsole.ProcessInput(submitText);
				EventSystem.current.SetSelectedGameObject(inputText.gameObject);
				inputText.OnPointerClick(new PointerEventData(EventSystem.current));
				recentCommandIndex = -1;
				lastInput = string.Empty;
				recentFillSearch.Clear();
			}
		}

		private enum HistoryInput
		{
			None, Up, Down
		}
	}
}