using NLog;
using System.Text;

namespace PictureEncoder
{
	public class ConsoleProgressBar : TextWriter, IProgress<double>, IDisposable
	{
		public override Encoding Encoding => Console.OutputEncoding;
		private TextWriter oldWriter;
		private Timer timer;
		private string name;
		private int textLeftPosition;
		private int textTopPosition;
		private double progress;

		private char[] animationChars = new char[] { '|', '/', '-', '\\', '|', '/', '-', '\\' };
		private int animationCharIndex = 0;
		private int barLength = 20;
		private Queue<string> waitingPrint = new Queue<string>();
		private DateTime startTime;

		private ILogger logger;

		public ConsoleProgressBar(string name, ILogger logger)
		{
			logger.Info($"开始进行{name}");
			oldWriter = Console.Out;
			Console.SetOut(this);
			this.name = name;
			this.logger = logger;
			(textLeftPosition, textTopPosition) = Console.GetCursorPosition();
			timer = new Timer(UpdateAnimation);
			timer.Change(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1d / animationChars.Length));
			startTime = DateTime.Now;
		}

		/// <summary>
		/// 报告进度
		/// </summary>
		/// <param name="value">0~1的范围</param>
		public void Report(double value) => progress = value;

		public void UpdateAnimation(object? state)
		{
			progress = Math.Min(Math.Max(0, progress), 1);  // 限定范围
			Console.SetCursorPosition(0, textLeftPosition == 0 ? textTopPosition : textTopPosition + 1);
			oldWriter.Write(new string(' ', barLength + 12));  // 清空这个任务栏
			Console.SetCursorPosition(textLeftPosition, textTopPosition);
			while (waitingPrint.Count > 0)
			{
				var text = waitingPrint.Dequeue();
				oldWriter.Write(text);
			}
			(textLeftPosition, textTopPosition) = Console.GetCursorPosition();
			var doneText = new string('#', (int)(progress * barLength));
			var barText = $"[{doneText}{new string('-', barLength - doneText.Length)}] {(progress * 100).ToString("F2")}% {animationChars[animationCharIndex]}";
			animationCharIndex++;
			if (animationCharIndex >= animationChars.Length) { animationCharIndex = 0; }
			Console.SetCursorPosition(0, textLeftPosition == 0 ? textTopPosition : textTopPosition + 1);
			oldWriter.Write(barText);
		}

		protected override void Dispose(bool disposing)
		{
			timer.Change(Timeout.Infinite, Timeout.Infinite);
			timer.Dispose();
			Console.SetCursorPosition(0, textLeftPosition == 0 ? textTopPosition : textTopPosition + 1);
			oldWriter.Write(new string(' ', barLength + 12));  // 清空这个任务栏
			Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
			Console.SetOut(oldWriter);
			logger.Info($"{name} 结束, 耗时 {(DateTime.Now - startTime).TotalSeconds.ToString("F2")}s");
		}

		public override void Write(string? value)
		{
			if (value == null) { return; }
			waitingPrint.Enqueue(value);
		}
	}
}
