//
// System.Web.Configuration.MachineKeySection
//
// Authors:
//	Chris Toshok (toshok@ximian.com)
//
// (c) Copyright 2005 Novell, Inc (http://www.novell.com)
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.ComponentModel;
using System.Configuration;
using System.Security.Cryptography;

#if NET_2_0

namespace System.Web.Configuration {

	public sealed class MachineKeySection : ConfigurationSection
	{
		static ConfigurationProperty decryptionProp;
		static ConfigurationProperty decryptionKeyProp;
		static ConfigurationProperty validationProp;
		static ConfigurationProperty validationKeyProp;
		static ConfigurationPropertyCollection properties;

		static MachineKeySection ()
		{
			decryptionProp = new ConfigurationProperty ("decryption", typeof (string), "Auto",
								    PropertyHelper.WhiteSpaceTrimStringConverter,
								    PropertyHelper.NonEmptyStringValidator,
								    ConfigurationPropertyOptions.None);
			decryptionKeyProp = new ConfigurationProperty ("decryptionKey", typeof (string), "AutoGenerate,IsolateApps",
								       PropertyHelper.WhiteSpaceTrimStringConverter,
								       PropertyHelper.NonEmptyStringValidator,
								       ConfigurationPropertyOptions.None);
			validationProp = new ConfigurationProperty ("validation", typeof (MachineKeyValidation), MachineKeyValidation.SHA1,
								    new MachineKeyValidationConverter (),
								    PropertyHelper.DefaultValidator,
								    ConfigurationPropertyOptions.None);
			validationKeyProp = new ConfigurationProperty ("validationKey", typeof (string), "AutoGenerate,IsolateApps",
								       PropertyHelper.WhiteSpaceTrimStringConverter,
								       PropertyHelper.NonEmptyStringValidator,
								       ConfigurationPropertyOptions.None);

			properties = new ConfigurationPropertyCollection ();

			properties.Add (decryptionProp);
			properties.Add (decryptionKeyProp);
			properties.Add (validationProp);
			properties.Add (validationKeyProp);

			AutoGenKeys ();
		}

		[MonoTODO]
		protected override void Reset (ConfigurationElement parentElement)
		{
			base.Reset (parentElement);
		}

		[TypeConverter (typeof (WhiteSpaceTrimStringConverter))]
		[StringValidator (MinLength = 1)]
		[ConfigurationProperty ("decryption", DefaultValue = "Auto")]
		public string Decryption {
			get { return (string) base [decryptionProp];}
			set { base[decryptionProp] = value; }
		}

		[TypeConverter (typeof (WhiteSpaceTrimStringConverter))]
		[StringValidator (MinLength = 1)]
		[ConfigurationProperty ("decryptionKey", DefaultValue = "AutoGenerate,IsolateApps")]
		public string DecryptionKey {
			get { return (string) base [decryptionKeyProp];}
			set { base[decryptionKeyProp] = value;  SetDecryptionKey (value); }
		}

		[TypeConverter (typeof (MachineKeyValidationConverter))]
		[ConfigurationProperty ("validation", DefaultValue = "SHA1")]
		public MachineKeyValidation Validation {
			get { return (MachineKeyValidation) base [validationProp];}
			set { base[validationProp] = value; }
		}

		[TypeConverter (typeof (WhiteSpaceTrimStringConverter))]
		[StringValidator (MinLength = 1)]
		[ConfigurationProperty ("validationKey", DefaultValue = "AutoGenerate,IsolateApps")]
		public string ValidationKey {
			get { return (string) base [validationKeyProp];}
			set { base[validationKeyProp] = value;  SetValidationKey (value); }
		}

		protected override ConfigurationPropertyCollection Properties {
			get { return properties; }
		}

#region CompatabilityCode
		static byte [] autogenerated;
		static byte [] autogenerated_decrypt;
		byte[] decryption_key;
		byte[] decryption_key_192bits;
		byte[] validation_key;

		static void AutoGenKeys ()
		{
			autogenerated = new byte [64];
			RandomNumberGenerator rng = RandomNumberGenerator.Create ();
			rng.GetBytes (autogenerated);
			autogenerated_decrypt = new byte [64];
			rng.GetBytes (autogenerated_decrypt);
		}

		static byte ToHexValue (char c, bool high)
		{
			byte v;
			if (c >= '0' && c <= '9')
				v = (byte) (c - '0');
			else if (c >= 'a' && c <= 'f')
				v = (byte) (c - 'a' + 10);
			else if (c >= 'A' && c <= 'F')
				v = (byte) (c - 'A' + 10);
			else
				throw new ArgumentException ("Invalid hex character");

			if (high)
				v <<= 4;

			return v;
		}
		
		internal static byte [] GetBytes (string key, int len)
		{
			byte [] result = new byte [len / 2];
			for (int i = 0; i < len; i += 2)
				result [i / 2] = (byte) (ToHexValue (key [i], true) + ToHexValue (key [i + 1], false));

			return result;
		}

		static byte [] MakeKey (string key, bool decryption) //, out bool isolate)
		{
			if (key == null || key.StartsWith ("AutoGenerate")){
				//isolate = key.IndexOf ("IsolateApps") != 1;

				return (decryption) ? autogenerated_decrypt : autogenerated;
			}

			//isolate = false;

			int len = key.Length;
			if (len < 40 || len > 128 || (len % 2) == 1)
				throw new ArgumentException ("Invalid key length");

			return GetBytes (key, len);
		}

		internal void SetDecryptionKey (string n)
		{
			decryption_key = MakeKey (n, true); //, out isolate_decryption);
			decryption_key_192bits = new byte [24];
			int count = 24;
			if (decryption_key.Length < 24)
				count = decryption_key.Length;
			Buffer.BlockCopy (decryption_key, 0, decryption_key_192bits, 0, count);
		}

		internal void SetValidationKey (string n)
		{
			validation_key = MakeKey (n, false); //, out isolate_validation);
		}
		
		internal byte [] ValidationKeyBytes {
			get {
				if (validation_key == null)
					SetValidationKey (ValidationKey);
				return validation_key;
			}
		}

		internal byte [] DecryptionKeyBytes {
			get {
				if (decryption_key == null)
					SetDecryptionKey (DecryptionKey);
				return decryption_key;
			}
		}

		internal byte [] DecryptionKey192Bits {
			get {
				if (decryption_key_192bits == null)
					SetDecryptionKey (DecryptionKey);
				return decryption_key_192bits;
			}
		}
#endregion

	}
}

#endif
