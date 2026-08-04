using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;

namespace RA2YR.Core.Formats.PackedMap
{
    internal sealed class StrictBase64Decoder
    {
        public StrictBase64DecodeResult Decode(string input, StrictBase64ReadLimits limits = null, StrictBase64Policy policy = StrictBase64Policy.StandardAlphabetNoWhitespace)
        {
            limits = limits ?? new StrictBase64ReadLimits();
            var diagnostics = new List<PackedMapDiagnostic>();
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (policy != StrictBase64Policy.StandardAlphabetNoWhitespace) throw new ArgumentOutOfRangeException(nameof(policy));
            if ((input.Length & 3) != 0)
            { diagnostics.Add(Error(PackedMapDiagnosticCode.InvalidBase64Length, "Base64 length must be divisible by four.")); return new StrictBase64DecodeResult(null, diagnostics); }

            int padding = 0;
            if (input.EndsWith("=", StringComparison.Ordinal)) padding++;
            if (input.EndsWith("==", StringComparison.Ordinal)) padding++;
            for (int index = 0; index < input.Length; index++)
            {
                char value = input[index];
                if (value == ' ' || value == '\t' || value == '\r' || value == '\n')
                { diagnostics.Add(Error(PackedMapDiagnosticCode.Base64Whitespace, "Whitespace is not accepted in strict Base64.")); break; }
                bool alphabet = value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '+' || value == '/' || value == '=';
                if (!alphabet || (value == '=' && index < input.Length - padding))
                { diagnostics.Add(Error(value == '=' ? PackedMapDiagnosticCode.InvalidBase64Padding : PackedMapDiagnosticCode.InvalidBase64Character, "The Base64 input contains an invalid character or padding position.")); break; }
            }
            if (diagnostics.Any(d => d.Severity == BinaryDiagnosticSeverity.Error)) return new StrictBase64DecodeResult(null, diagnostics);
            long decodedLength = checked((long)(input.Length / 4) * 3 - padding);
            if (decodedLength < 0 || decodedLength > limits.MaxDecodedBytes || decodedLength > int.MaxValue)
            { diagnostics.Add(Error(PackedMapDiagnosticCode.Base64OutputBudgetExceeded, "Decoded Base64 output exceeds the configured budget.")); return new StrictBase64DecodeResult(null, diagnostics); }
            try
            {
                byte[] bytes = Convert.FromBase64String(input);
                if (bytes.Length != decodedLength) { diagnostics.Add(Error(PackedMapDiagnosticCode.Base64DecodeFailure, "The Base64 primitive returned an unexpected length.")); return new StrictBase64DecodeResult(null, diagnostics); }
                return new StrictBase64DecodeResult(bytes, diagnostics);
            }
            catch (FormatException)
            { diagnostics.Add(Error(PackedMapDiagnosticCode.Base64DecodeFailure, "The Base64 input failed strict decoding.")); return new StrictBase64DecodeResult(null, diagnostics); }
        }
        private static PackedMapDiagnostic Error(PackedMapDiagnosticCode code, string message) => new PackedMapDiagnostic(code, BinaryDiagnosticSeverity.Error, message);
    }
}
