using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Security.Cryptography;
using System.Text;

namespace PictureEncoder
{
    public static class PicEncoder
    {
		/// <summary>
		/// 加密图片
		/// </summary>
		/// <param name="password">加密使用的密码</param>
		/// <param name="fileInput">待加密图片的二进制流</param>
		/// <param name="progress">进度报告器</param>
		/// <exception cref="UnknownImageFormatException">未知的图片格式</exception>
		/// <returns>编码后的png二进制流</returns>
        public static async Task<MemoryStream> Encode(string password, Stream fileInput, IProgress<double>? progress = null)
        {
			return await Task.Run(() =>
			{
				var passwordInts = ComputePasswordHash(password);
				using var image = Image.Load<Rgba32>(fileInput);
				var totalPixel = image.Width * image.Height;
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
							progress?.Report((double)(y * image.Width + x + 1) / totalPixel);
						}
					}
				});
				var outputStream = new MemoryStream();
				image.SaveAsPng(outputStream);
				outputStream.Seek(0, SeekOrigin.Begin);
				return outputStream;
			});
		}

		/// <summary>
		/// 解密图片
		/// </summary>
		/// <param name="password">解密使用的密码</param>
		/// <param name="fileInput">待解密图片的二进制流</param>
		/// <param name="progress">进度报告器</param>
		/// <exception cref="UnknownImageFormatException">未知的图片格式</exception>
		/// <returns>编码后的png二进制流</returns>
		public static async Task<MemoryStream> Decode(string password, Stream fileInput, IProgress<double>? progress = null)
        {
			return await Task.Run(() =>
			{
				var passwordInts = ComputePasswordHash(password);
				using var image = Image.Load<Rgba32>(fileInput);
				var totalPixel = image.Width * image.Height;
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
							progress?.Report((double)(y * image.Width + x + 1) / totalPixel);
						}
					}
				});
				var outputStream = new MemoryStream();
				image.SaveAsPng(outputStream);
				outputStream.Seek(0, SeekOrigin.Begin);
				return outputStream;
			});
        }

		private static int[] ComputePasswordHash(string password)
        {
			using var mySHA256 = SHA256.Create();
			var passwordBytes = mySHA256.ComputeHash(Encoding.UTF8.GetBytes(password));
			var passwordInts = new int[8];
			for (int i = 0; i < passwordInts.Length; i++)
			{
				passwordInts[i] = BitConverter.ToInt32(passwordBytes, i * 4);
			}
			return passwordInts;
		}
    }
}