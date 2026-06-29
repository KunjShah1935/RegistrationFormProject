using Microsoft.AspNetCore.Mvc;
using SkiaSharp;

namespace RegistrationFormProject.Controllers
{
    public class CaptchaController : Controller
    {
        public IActionResult GenerateCaptcha()
        {
            string chars =
                "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

            Random random = new Random();

            string captcha =
                new string(
                    Enumerable.Repeat(chars, 5)
                    .Select(s => s[random.Next(s.Length)])
                    .ToArray());

            HttpContext.Session.SetString(
                "Captcha",
                captcha);

            using var bitmap =
                new SKBitmap(200, 70);

            using var canvas =
                new SKCanvas(bitmap);

            // Semi-transparent white background for glassmorphic visual integration
            canvas.Clear(new SKColor(255, 255, 255, 220));

            SKColor[] colors =
            {
                new SKColor(16, 185, 129), // Emerald
                new SKColor(37, 99, 235),  // Royal Blue
                new SKColor(124, 58, 237), // Purple
                new SKColor(220, 38, 38),  // Red
                new SKColor(15, 23, 42)    // Slate Dark
            };

            // Noise dots
            for (int i = 0; i < 400; i++)
            {
                using var dotPaint = new SKPaint
                {
                    Color = colors[random.Next(colors.Length)].WithAlpha(80),
                    StrokeWidth = 1,
                    IsAntialias = true
                };

                canvas.DrawPoint(
                    random.Next(bitmap.Width),
                    random.Next(bitmap.Height),
                    dotPaint);
            }

            // Artistic noise bezier curves for premium visual look and better bot protection
            for (int i = 0; i < 4; i++)
            {
                using var linePaint = new SKPaint
                {
                    Color = colors[random.Next(colors.Length)].WithAlpha(100),
                    StrokeWidth = random.Next(2, 4),
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true
                };

                using var path = new SKPath();
                path.MoveTo(0, random.Next(bitmap.Height));
                path.CubicTo(
                    random.Next(bitmap.Width / 3), random.Next(bitmap.Height),
                    random.Next(bitmap.Width * 2 / 3), random.Next(bitmap.Height),
                    bitmap.Width, random.Next(bitmap.Height));

                canvas.DrawPath(path, linePaint);
            }

            // Characters using modern SKFont to avoid obsolete warnings
            for (int i = 0; i < captcha.Length; i++)
            {
                using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
                using var font = new SKFont(typeface, random.Next(32, 42));
                using var textPaint = new SKPaint
                {
                    Color = colors[random.Next(colors.Length)],
                    IsAntialias = true
                };

                float x = 18 + (i * 35);
                float y = random.Next(42, 54);

                canvas.Save();

                canvas.RotateDegrees(
                    random.Next(-15, 15),
                    x,
                    y);

                canvas.DrawText(
                    captcha[i].ToString(),
                    x,
                    y,
                    font,
                    textPaint);

                canvas.Restore();
            }

            using var image =
                SKImage.FromBitmap(bitmap);

            using var data =
                image.Encode(
                    SKEncodedImageFormat.Png,
                    100);

            return File(
                data.ToArray(),
                "image/png");
        }
    }
}