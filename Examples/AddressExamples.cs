// Copyright 2026 tato-natenadze
// SPDX-License-Identifier: Apache-2.0

using Ripple;

namespace RippleExamples
{
    public static class AddressExamples
    {
        public static bool ValidateClassic(string address)
        {
            return RippleClient.ValidateAddress(address);
        }

        public static string Encode(string classicAddress, uint? tag, bool isTestNetwork)
        {
            // null means no tag; 0 is a valid tag. true covers Testnet/Devnet.
            return XAddressCodec.Encode(classicAddress, tag, isTestNetwork);
        }

        public static XAddressInfo Decode(string xAddress)
        {
            // Returns ClassicAddress, DestinationTag and IsTestNetwork.
            // An invalid X-address throws ArgumentException.
            return XAddressCodec.Decode(xAddress);
        }

        public static bool ValidateX(string xAddress)
        {
            return XAddressCodec.IsValid(xAddress);
        }
    }
}
