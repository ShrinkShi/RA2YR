using System;
using NUnit.Framework;
using RA2YR.Core.Formats.Mix.Crypto;

namespace RA2YR.Tests.EditMode.Formats.Mix
{
    [TestFixture]
    public sealed class MixCryptoTests
    {
        [TestCase(
            "0000000000000000",
            "0000000000000000",
            "4EF997456198DD78")]
        [TestCase(
            "FFFFFFFFFFFFFFFF",
            "FFFFFFFFFFFFFFFF",
            "51866FD5B85ECB8A")]
        [TestCase(
            "3000000000000000",
            "1000000000000001",
            "7D856F9A613063F2")]
        public void StandardBigEndianBlocksMatchSchneierVectors(
            string keyHex,
            string plaintextHex,
            string ciphertextHex)
        {
            var cipher = new BlowfishCipher(Hex(keyHex));
            var encrypted = new byte[BlowfishCipher.BlockSize];
            var decrypted = new byte[BlowfishCipher.BlockSize];

            cipher.EncryptBigEndianBlock(Hex(plaintextHex), encrypted);
            cipher.DecryptBigEndianBlock(encrypted, decrypted);

            Assert.That(encrypted, Is.EqualTo(Hex(ciphertextHex)));
            Assert.That(decrypted, Is.EqualTo(Hex(plaintextHex)));
        }

        [Test]
        public void WestwoodAdapterMakesLittleEndianHostWordConversionExplicit()
        {
            var cipher = new BlowfishCipher(Hex("0000000000000000"));
            byte[] plaintext = Hex("0000000000000000");
            var standard = new byte[BlowfishCipher.BlockSize];
            var westwood = new byte[BlowfishCipher.BlockSize];

            cipher.EncryptBigEndianBlock(plaintext, standard);
            cipher.EncryptWestwoodLittleEndianWordBlock(plaintext, westwood);

            Assert.That(westwood, Is.EqualTo(standard));
            Assert.That(westwood, Is.EqualTo(Hex("4EF997456198DD78")));
        }

        [Test]
        public void KeySourceVectorDerivesExpectedFiftySixByteKey()
        {
            byte[] keySource = Hex(
                "02000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "03000000000000000000000000000000000000000000000000000000000000000000000000000000");

            WestwoodMixKeyDerivationResult result =
                WestwoodMixKeyDeriver.Derive(keySource);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                result.GetKeyMaterial(),
                Is.EqualTo(Hex(
                    "146C6C9CA162964EA0CCB073A24F49A3CFE3C0D2F428C717387596B009B39C4E" +
                    "125F48F733C399A13422415275A8498A2002DB16EF604144")));
        }

        [Test]
        public void DerivedKeyEncryptsWestwoodDirectoryVector()
        {
            byte[] keySource = Hex(
                "02000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "03000000000000000000000000000000000000000000000000000000000000000000000000000000");
            byte[] key = WestwoodMixKeyDeriver.Derive(keySource).GetKeyMaterial();
            var cipher = new BlowfishCipher(key);
            byte[] plaintext = Hex(
                "010003000000443322110000000003000000000000000000");
            var encrypted = new byte[plaintext.Length];

            for (int offset = 0; offset < plaintext.Length; offset += BlowfishCipher.BlockSize)
            {
                cipher.EncryptWestwoodLittleEndianWordBlock(
                    plaintext.AsSpan(offset, BlowfishCipher.BlockSize),
                    encrypted.AsSpan(offset, BlowfishCipher.BlockSize));
            }

            Assert.That(
                encrypted,
                Is.EqualTo(Hex(
                    "BE78E7D244A51CAB4B8F387E6A978CE124BA67967F4E3AD6")));
        }

        [Test]
        public void SyntheticKeySourceSupportsEncryptDecryptRoundTrip()
        {
            var keySource = new byte[WestwoodMixKeyDeriver.KeySourceLength];
            keySource[0] = 2;
            keySource[WestwoodMixKeyDeriver.CiphertextBlockLength] = 3;
            byte[] key = WestwoodMixKeyDeriver.Derive(keySource).GetKeyMaterial();
            var cipher = new BlowfishCipher(key);
            byte[] plaintext = Hex("0123456789ABCDEF");
            var encrypted = new byte[BlowfishCipher.BlockSize];
            var decrypted = new byte[BlowfishCipher.BlockSize];

            cipher.EncryptWestwoodLittleEndianWordBlock(plaintext, encrypted);
            cipher.DecryptWestwoodLittleEndianWordBlock(encrypted, decrypted);

            Assert.That(decrypted, Is.EqualTo(plaintext));
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(57)]
        public void InvalidBlowfishKeyLengthsHaveSpecificDiagnostics(int length)
        {
            MixCryptoException exception = Assert.Throws<MixCryptoException>(
                () => new BlowfishCipher(new byte[length]));

            Assert.That(
                exception.Diagnostic.Code,
                Is.EqualTo(MixCryptoDiagnosticCode.InvalidBlowfishKeyLength));
            Assert.That(exception.Diagnostic.Field, Is.EqualTo("key"));
        }

        [Test]
        public void FourAndFiftySixByteKeysAreAccepted()
        {
            Assert.DoesNotThrow(() => new BlowfishCipher(new byte[4]));
            Assert.DoesNotThrow(() => new BlowfishCipher(new byte[56]));
        }

        [TestCase(0)]
        [TestCase(79)]
        [TestCase(81)]
        public void InvalidKeySourceLengthsReturnSpecificDiagnostics(int length)
        {
            WestwoodMixKeyDerivationResult result =
                WestwoodMixKeyDeriver.Derive(new byte[length]);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                result.Diagnostic.Code,
                Is.EqualTo(MixCryptoDiagnosticCode.InvalidKeySourceLength));
            Assert.That(result.Diagnostic.BlockIndex, Is.EqualTo(-1));
        }

        [Test]
        public void KeySourceValueEqualToModulusIsRejected()
        {
            var keySource = new byte[WestwoodMixKeyDeriver.KeySourceLength];
            Hex(
                "157F43AA3D4FFBD1E6C1B0F86A0EDDAB4AB08266FA54AAE8" +
                "A23F7151D6605156E4FC396D08DABC51").CopyTo(keySource, 0);

            WestwoodMixKeyDerivationResult result =
                WestwoodMixKeyDeriver.Derive(keySource);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                result.Diagnostic.Code,
                Is.EqualTo(MixCryptoDiagnosticCode.KeySourceValueOutOfRange));
            Assert.That(result.Diagnostic.BlockIndex, Is.EqualTo(0));
        }

        [Test]
        public void DerivedKeyResultDoesNotExposeMutableState()
        {
            var keySource = new byte[WestwoodMixKeyDeriver.KeySourceLength];
            WestwoodMixKeyDerivationResult result =
                WestwoodMixKeyDeriver.Derive(keySource);
            byte[] first = result.GetKeyMaterial();
            first[0] ^= 0xff;

            byte[] second = result.GetKeyMaterial();

            Assert.That(second[0], Is.Not.EqualTo(first[0]));
            Assert.That(second.Length, Is.EqualTo(56));
        }

        [Test]
        public void InvalidBlockLengthsHaveSpecificDiagnostics()
        {
            var cipher = new BlowfishCipher(new byte[8]);
            MixCryptoException inputError = Assert.Throws<MixCryptoException>(
                () => cipher.EncryptBigEndianBlock(
                    new byte[7],
                    new byte[BlowfishCipher.BlockSize]));
            MixCryptoException outputError = Assert.Throws<MixCryptoException>(
                () => cipher.EncryptBigEndianBlock(
                    new byte[BlowfishCipher.BlockSize],
                    new byte[9]));

            Assert.That(
                inputError.Diagnostic.Code,
                Is.EqualTo(MixCryptoDiagnosticCode.InvalidBlockLength));
            Assert.That(
                outputError.Diagnostic.Code,
                Is.EqualTo(MixCryptoDiagnosticCode.InvalidBlockLength));
        }

        private static byte[] Hex(string value)
        {
            if (value == null || (value.Length & 1) != 0)
            {
                throw new ArgumentException("Hex input must have an even length.", nameof(value));
            }

            var bytes = new byte[value.Length / 2];
            for (int index = 0; index < bytes.Length; index++)
            {
                bytes[index] = Convert.ToByte(value.Substring(index * 2, 2), 16);
            }

            return bytes;
        }
    }
}
