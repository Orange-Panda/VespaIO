using JetBrains.Annotations;
using System.Collections.Generic;

namespace LMirman.VespaIO
{
	[PublicAPI]
	public class NativeConsole : Console
	{
		public readonly HashSet<string> autofillExclusions = new HashSet<string>();
		private string lastAutofillPreview;

		private string virtualText;
		public string VirtualText
		{
			get => virtualText;
			set
			{
				autofillExclusions.Clear();
				virtualText = value;
				UpdateNextAutofill();
			}
		}

		/// <summary>
		/// If <see cref="ApplyNextAutofill"/> were to be invoked, this auto fill value will be input into <see cref="VirtualText"/> and another class 
		/// </summary>
		public AutofillValue NextAutofill { get; private set; }

		public override void RunInvocation(Invocation invocation)
		{
#if UNITY_EDITOR
			// Automatically enable cheats if configured to do so in the config, making quick debugging more convenient when enabled.
			if (invocation.command.Cheat && !cheatsEnabled && NativeSettings.Config.editorAutoEnableCheats)
			{
				Log("<color=yellow>Cheats have automatically been enabled.</color>");
				cheatsEnabled = true;
			}
#endif

			base.RunInvocation(invocation);
		}

		public void EnableCheats()
		{
			DevConsole.Log("Cheats have been enabled!", LogStyling.Info);
			cheatsEnabled = true;
		}

		private void UpdateNextAutofill()
		{
			NextAutofill = GetAutofillValue(virtualText, autofillExclusions);
		}

		public bool ApplyNextAutofill(out string newInputValue)
		{
			if (NextAutofill == null)
			{
				autofillExclusions.Clear();
				UpdateNextAutofill();
			}

			if (NextAutofill != null)
			{
				AutofillValue autofillValue = NextAutofill;
				autofillExclusions.Add(autofillValue.newWord);
				UpdateNextAutofill();
				newInputValue = $"{virtualText.Substring(0, autofillValue.globalStartIndex)}{autofillValue.newWord} ";
				return true;
			}
			else
			{
				newInputValue = virtualText;
				return false;
			}
		}
	}
}