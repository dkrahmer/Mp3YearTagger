using System;

namespace KrahmerSoft.Mp3YearTagger
{
	public class VerboseInfo
	{
		public int VerboseLevel { get; private set; }
		public bool IsError { get; private set; }
		private string _message;
		private Func<string> _getMessage;
		public string Message
		{
			get
			{
				if (_getMessage != null)
				{
					_message = _getMessage();
					_getMessage = null;
				}

				return _message;
			}
		}

		public VerboseInfo(int verboseLevel, string message, bool isError = false)
		{
			VerboseLevel = verboseLevel;
			_message = message;
			IsError = isError;
		}

		public VerboseInfo(int verboseLevel, Func<string> getMessage, bool isError = false)
		{
			VerboseLevel = verboseLevel;
			_getMessage = getMessage;
			IsError = isError;
		}

	}
}