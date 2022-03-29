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
		[SerializeField] private GameObject container;
		[SerializeField] private TextMeshProUGUI history;
		[SerializeField] private TMP_InputField inputText;

		private bool historyDirty;
		private bool hasNoEventSystem;
		private GameObject previousSelectable;

		internal static DevConsoleRunner Instance { get; private set; }

		private void OnEnable()
		{
			DevConsole.HistoryUpdate += DeveloperConsole_HistoryUpdate;
		}

		private void OnDisable()
		{
			DevConsole.HistoryUpdate -= DeveloperConsole_HistoryUpdate;
		}

		private void DeveloperConsole_HistoryUpdate()
		{
			historyDirty = true;
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
			bool openKey = DidInput(config.openConsoleKeycodes);
			bool exitKey = DidInput(config.closeConsoleKeycodes) || (config.closeConsoleOnLeftClick && Input.GetMouseButtonDown(0));

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
				history.text = DevConsole.history.ToString();
				historyDirty = false;
			}
		}

		private bool DidInput(KeyCode[] keyCodes)
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
				DevConsole.ProcessCommand(submitText);
				EventSystem.current.SetSelectedGameObject(inputText.gameObject);
				inputText.OnPointerClick(new PointerEventData(EventSystem.current));
			}
		}
	}
}