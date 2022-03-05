using NLog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Security.Cryptography;
using System.Text;

namespace PictureEncoder
{
	public class Program
	{
		private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
		private static bool _debug = false;

		public static ILogger Logger => _logger;
		public static bool Debug
		{
			get => _debug;
			set
			{
				_debug = value;
				LogManager.Configuration.Variables["consoleMinLevel"] = value ? "Debug" : "Info";
			}
		}


		public static void Main(string[] args)
		{
			Logger.Info("开始运行");
			var rootCommand = new RootCommand();
			var encodeCommand = new EncodeCommand(Logger);
			var decodeCommand = new DecodeCommand(Logger);

			rootCommand.AddCommand(encodeCommand);
			rootCommand.AddCommand(decodeCommand);

			rootCommand.Invoke(args);
			/*Console.WriteLine("程序已退出，按回车关闭窗口...");
			Console.ReadLine();*/
		}
	}

	public class CommandOption
	{
		public FileInfo? File { get; set; } = null;
		public string Password { get; set; } = string.Empty;
		public FileInfo? OutPath { get; set; } = null;
		public bool Debug { get; set; } = false;
	}

	public class CommandBinder : BinderBase<CommandOption>
	{
		public readonly Argument<FileInfo> _fileArgument = new("file", description: "待处理文件");
		public readonly Option<string> _passwordOption = new(new string[] { "-p" }, description: "密码");
		public readonly Option<FileInfo> _outPathOption = new(new string[] { "-o" }, description: "输出文件名");
		public readonly Option<bool> _debugOption = new(new string[] { "-debug" }, description: "debug模式");

		protected override CommandOption GetBoundValue(BindingContext bindingContext) => new()
		{
			File = bindingContext.ParseResult.GetValueForArgument(_fileArgument),
			Password = bindingContext.ParseResult.GetValueForOption(_passwordOption) ?? string.Empty,
			OutPath = bindingContext.ParseResult.GetValueForOption(_outPathOption),
			Debug = bindingContext.ParseResult.GetValueForOption(_debugOption)
		};
	}

	#region Commands
	public class EncodeCommand : Command
	{
		private readonly CommandBinder _binder;
		private readonly ILogger _logger;

		public CommandBinder Binder => _binder;

		public EncodeCommand(ILogger logger) : base("encode", "加密图片")
		{
			_binder = new CommandBinder();
			_logger = logger;
			AddArgument(_binder._fileArgument);
			AddOption(_binder._passwordOption);
			AddOption(_binder._outPathOption);
			this.SetHandler<CommandOption>(CommandHandler, Binder);
		}

		public void CommandHandler(CommandOption options)
		{
			Program.Debug = options.Debug;
			if (options.File == null || !options.File.Exists)
			{
				_logger.Error("待加密图片不存在");
				return;
			}
			if (options.OutPath == null)
			{
				_logger.Debug($"未定义输出路径，使用默认路径");
				var path = options.File.Name.Substring(0, options.File.Name.LastIndexOf('.')) + "_encoded.png";
				options.OutPath = new FileInfo(path);
			}
			using var mySHA256 = SHA256.Create();
			var passwordBytes = mySHA256.ComputeHash(Encoding.UTF8.GetBytes(options.Password));
			var passwordInts = new int[8];
			for (int i = 0; i < passwordInts.Length; i++)
			{
				passwordInts[i] = BitConverter.ToInt32(passwordBytes, i * 4);
			}
			_logger.Info($"正在加载图片 {options.File.Name}");
			_logger.Debug($"输出路径 {options.OutPath.FullName}");
			_logger.Debug($"密码: {options.Password} 0x" + string.Join(' ', passwordBytes.Select(b => b.ToString("X2"))));
			using var image = Image.Load<Rgba32>(options.File.FullName);
			var totalPixel = image.Width * image.Height;
			_logger.Info($"图片加载成功，尺寸: {image.Width}x{image.Height}, 总计 {totalPixel}px");
			var progressBar = new ConsoleProgressBar("加密图片", _logger);
			var index = 0;
			image.ProcessPixelRows(accessor =>
			{
				int hash = passwordInts[index];
				for (int y = 0; y < image.Height; y++)
				{
					var row = accessor.GetRowSpan(y);

					for (int x = 0; x < image.Width; x++)
					{
						hash = hash * 31 + passwordInts[index] * x * y;
						index = (index + 1) % 8;
						var bytes = BitConverter.GetBytes(hash);
						row[x].R += bytes[0];
						row[x].G += bytes[1];
						row[x].B += bytes[2];
						row[x].A = 255;
						progressBar.Report((double)(y * image.Width + x + 1) / totalPixel);
					}
				}
				progressBar.Dispose();
			});
			_logger.Info($"加密完毕，储存到: {options.OutPath.Name}");
			image.SaveAsPng(options.OutPath.FullName);
		}
	}
	public class DecodeCommand : Command
	{
		private readonly CommandBinder _binder;
		private readonly ILogger _logger;

		public CommandBinder Binder => _binder;

		public DecodeCommand(ILogger logger) : base("decode", "解密图片")
		{
			_binder = new CommandBinder();
			_logger = logger;
			AddArgument(_binder._fileArgument);
			AddOption(_binder._passwordOption);
			AddOption(_binder._outPathOption);
			this.SetHandler<CommandOption>(CommandHandler, Binder);
		}

		public void CommandHandler(CommandOption options)
		{
			Program.Debug = options.Debug;
			if (options.File == null || !options.File.Exists)
			{
				_logger.Error("待解密图片不存在");
				return;
			}
			if (options.OutPath == null)
			{
				_logger.Debug($"未定义输出路径，使用默认路径");
				var fileName = options.File.Name.Substring(0, options.File.Name.LastIndexOf('.'));
				var path = (fileName.EndsWith("_encoded") ? fileName.Substring(0, fileName.Length - 8) : fileName) + "_decoded.png";
				options.OutPath = new FileInfo(path);
			}
			using var mySHA256 = SHA256.Create();
			var passwordBytes = mySHA256.ComputeHash(Encoding.UTF8.GetBytes(options.Password));
			var passwordInts = new int[8];
			for (int i = 0; i < passwordInts.Length; i++)
			{
				passwordInts[i] = BitConverter.ToInt32(passwordBytes, i * 4);
			}
			_logger.Info($"正在加载图片 {options.File.Name}");
			_logger.Debug($"输出路径 {options.OutPath.FullName}");
			_logger.Debug($"密码: {options.Password} 0x" + string.Join(' ', passwordBytes.Select(b => b.ToString("X2"))));
			using var image = Image.Load<Rgba32>(options.File.FullName);
			var totalPixel = image.Width * image.Height;
			_logger.Info($"图片加载成功，尺寸: {image.Width}x{image.Height}, 总计 {totalPixel}px");
			var progressBar = new ConsoleProgressBar("解密图片", _logger);
			var index = 0;
			image.ProcessPixelRows(accessor =>
			{
				int hash = passwordInts[index];
				for (int y = 0; y < image.Height; y++)
				{
					var row = accessor.GetRowSpan(y);

					for (int x = 0; x < image.Width; x++)
					{
						hash = hash * 31 + passwordInts[index] * x * y;
						index = (index + 1) % 8;
						var bytes = BitConverter.GetBytes(hash);
						row[x].R -= bytes[0];
						row[x].G -= bytes[1];
						row[x].B -= bytes[2];
						row[x].A = 255;
						progressBar.Report((double)(y * image.Width + x + 1) / totalPixel);
					}
				}
				progressBar.Dispose();
			});
			_logger.Info($"解密完毕，储存到: {options.OutPath.Name}");
			image.SaveAsPng(options.OutPath.FullName);
		}
	}
	#endregion
}