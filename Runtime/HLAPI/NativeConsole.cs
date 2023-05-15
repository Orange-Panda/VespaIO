using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.VespaIO
{
	/// <summary>
	/// A standard implementation of <see cref="Console"/> that provides useful default functionality that isn't explicitly required.
	/// </summary>
	[PublicAPI]
	public class NativeConsole : Console
	{
		public readonly HashSet<string> autofillExclusions = new HashSet<string>();
		private string lastAutofillPreview;

		private string virtualText;
		/// <summary>
		/// Virtual text is the internal input text that is considered the current user console input.
		/// This is particularly important when using autofill because autofill will <b>not</b> change the virtual text until the user inputs another character.
		/// </summary>
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
		/// If <see cref="TryGetNextAutofillApplied"/> were to be invoked, this auto fill value will be input into <see cref="VirtualText"/> and another class 
		/// </summary>
		public AutofillValue NextAutofill { get; private set; }

		/// <summary>
		/// Run an invocation in the context of this console.
		/// </summary>
		/// <param name="invocation">The invocation to run on this console.</param>
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

		/// <summary>
		/// Immediately apply cheats to this session.
		/// This is irreversible.
		/// </summary>
		public void EnableCheats()
		{
			DevConsole.Log("Cheats have been enabled!", LogStyling.Info);
			cheatsEnabled = true;
		}

		private void UpdateNextAutofill()
		{
			AutofillValue autofillValue = GetAutofillValue(virtualText, autofillExclusions, Application.isEditor && NativeSettings.Config.editorAutoEnableCheats);
			if (autofillValue == null && autofillExclusions.Count > 0)
			{
				autofillExclusions.Clear();
				autofillValue = GetAutofillValue(virtualText, autofillExclusions, Application.isEditor && NativeSettings.Config.editorAutoEnableCheats);
			}

			NextAutofill = autofillValue;
		}

		/// <summary>
		/// Attempts to get a new value for the console input with <see cref="NextAutofill"/> applied.
		/// </summary>
		/// <param name="newInputValue">The new input value for the console after the autofill is applied.</param>
		/// <returns>True if there was an autofill to apply, false if there was not.</returns>
		public bool TryGetNextAutofillApplied(out string newInputValue)
		{
			if (NextAutofill != null)
			{
				AutofillValue autofillValue = NextAutofill;
				autofillExclusions.Add(autofillValue.newWord);
				UpdateNextAutofill();
				newInputValue = $"{virtualText.Substring(0, autofillValue.globalStartIndex)}{autofillValue.markupNewWord}";
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