using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Security.Cryptography;
using System.Text;

namespace PictureEncoder
{
    public static class Encoder
    {
        public static async Task<MemoryStream> Encode(string password, Stream fileInput, IProgress<double> progress)
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
							progress.Report((double)(y * image.Width + x + 1) / totalPixel);
						}
					}
				});
				var outputStream = new MemoryStream();
				image.SaveAsPng(outputStream);
				return outputStream;
			});
		}

		public static async Task<MemoryStream> Decode(string password, Stream fileInput, IProgress<double> progress)
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
							progress.Report((double)(y * image.Width + x + 1) / totalPixel);
						}
					}
				});
				using var outputStream = new MemoryStream();
				image.SaveAsPng(outputStream);
				return outputStream;
			});
        }

		public static int[] ComputePasswordHash(string password)
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