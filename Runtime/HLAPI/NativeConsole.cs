namespace LMirman.VespaIO
{
	public class NativeConsole : Console
	{
		public override void RunInvocation(Invocation invocation)
		{
#if UNITY_EDITOR
			// Automatically enable cheats if configured to do so in the config, making quick debugging more convenient when enabled.
			if (invocation.command.Cheat && !cheatsEnabled && ConsoleSettings.Config.editorAutoEnableCheats)
			{
				Log("<color=yellow>Cheats have automatically been enabled.</color>");
				cheatsEnabled = true;
			}
#endif
			
			base.RunInvocation(invocation);
		}

		public void EnableCheats()
		{
			DevConsole.Log("Cheats have been enabled!", Console.LogStyling.Info);
			cheatsEnabled = true;
		}
	}
}