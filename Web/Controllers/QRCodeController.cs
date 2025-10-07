using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing.Imaging;
using System.Drawing;

namespace Web.Controllers
{
    public class QRCodeController : Controller
    {
        [HttpGet("generate-qr")]

        public IActionResult GenerateQRCode(string url , string name)
        {
            if (string.IsNullOrEmpty(url))
            {
                return BadRequest("URL cannot be empty");
            }

            // Generate QR Code
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

            // Use PngByteQRCode for direct byte generation
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeBytes = qrCode.GetGraphic(300);

                // Return File for Download
                return File(qrCodeBytes, "image/png", $"{name}_qr_code_{Guid.NewGuid()}.png");
            }
        }
    }
}
