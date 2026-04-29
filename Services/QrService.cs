using QRCoder;

namespace PeluCorte.Services;

/// <summary>
/// Genera códigos QR como PNG en memoria. Sin dependencias externas
/// (QRCoder es 100% C#, no llama a ningún servicio).
/// </summary>
public class QrService
{
    /// <summary>
    /// Crea un PNG con el QR del contenido dado.
    /// </summary>
    /// <param name="contenido">Texto a codificar (típicamente una URL).</param>
    /// <param name="pixelesPorModulo">Tamaño de cada celda. 10 ≈ ~330x330px, 20 ≈ ~660x660px.</param>
    public byte[] GenerarPng(string contenido, int pixelesPorModulo = 12)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            throw new ArgumentException("El contenido del QR no puede estar vacío.", nameof(contenido));

        using var generator = new QRCodeGenerator();
        // ECCLevel.Q permite hasta un 25% del QR dañado y siga siendo legible — buen
        // compromiso para impresión sobre cristal, escaparates etc.
        using var data = generator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(pixelesPorModulo);
    }
}
