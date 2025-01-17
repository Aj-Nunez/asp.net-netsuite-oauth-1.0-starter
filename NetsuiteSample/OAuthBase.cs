﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace NetsuiteSample
{
	public static class OAuthBase
	{
		public enum SignatureTypes
		{
			HMACSHA256,
			PLAINTEXT,
			HMACSHA1
		}

		private static readonly Dictionary<SignatureTypes, string> SignatureTypeNames = new Dictionary<SignatureTypes, string>()
		{
			{ SignatureTypes.HMACSHA1, "HMAC-SHA1" },
			{ SignatureTypes.HMACSHA256, "HMAC-SHA256" },
			{ SignatureTypes.PLAINTEXT, "PLAINTEXT" }
		};

		/// <summary>
		/// Provides an internal structure to sort the query parameter
		/// </summary>
		protected class QueryParameter
		{
			private string name = null;
			private string value = null;

			public QueryParameter(string name, string value)
			{
				this.name = name;
				this.value = value;
			}

			public string Name
			{
				get { return name; }
			}

			public string Value
			{
				get { return value; }
			}
		}

		/// <summary>
		/// Comparer class used to perform the sorting of the query parameters
		/// </summary>
		protected class QueryParameterComparer : IComparer<QueryParameter>
		{

			#region IComparer<QueryParameter> Members

			public int Compare(QueryParameter x, QueryParameter y)
			{
				if (x.Name == y.Name)
				{
					return string.Compare(x.Value, y.Value);
				}
				else
				{
					return string.Compare(x.Name, y.Name);
				}
			}

			#endregion
		}

		public const string OAuthVersion = "1.0";
		public const string OAuthParameterPrefix = "oauth_";

		//
		// List of know and used oauth parameters' names
		//        
		public const string OAuthConsumerKeyKey = "oauth_consumer_key";
		public const string OAuthCallbackKey = "oauth_callback";
		public const string OAuthVersionKey = "oauth_version";
		public const string OAuthSignatureMethodKey = "oauth_signature_method";
		public const string OAuthSignatureKey = "oauth_signature";
		public const string OAuthTimestampKey = "oauth_timestamp";
		public const string OAuthNonceKey = "oauth_nonce";
		public const string OAuthTokenKey = "oauth_token";
		public const string OAuthTokenSecretKey = "oauth_token_secret";
		public const string HMACSHA256SignatureType = "HMAC-SHA256";
		public const string PlainTextSignatureType = "PLAINTEXT";

		public const string unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

		/// <summary>
		/// Helper function to compute a hash value
		/// </summary>
		/// <param name="hashAlgorithm">The hashing algoirhtm used. If that algorithm needs some initialization, like HMAC and its derivatives, they should be initialized prior to passing it to this function</param>
		/// <param name="data">The data to hash</param>
		/// <returns>a Base64 string of the hash value</returns>
		private static string ComputeHash(HashAlgorithm hashAlgorithm, string data)
		{
			if (hashAlgorithm == null)
			{
				throw new ArgumentNullException("hashAlgorithm");
			}

			if (string.IsNullOrEmpty(data))
			{
				throw new ArgumentNullException("data");
			}

			byte[] dataBuffer = System.Text.Encoding.ASCII.GetBytes(data);
			byte[] hashBytes = hashAlgorithm.ComputeHash(dataBuffer);

			return Convert.ToBase64String(hashBytes);
		}

		/// <summary>
		/// Internal function to cut out all non oauth query string parameters (all parameters not begining with "oauth_")
		/// </summary>
		/// <param name="parameters">The query string part of the Url</param>
		/// <returns>A list of QueryParameter each containing the parameter name and value</returns>
		private static List<QueryParameter> GetQueryParameters(string parameters)
		{
			if (parameters.StartsWith("?"))
			{
				parameters = parameters.Remove(0, 1);
			}

			List<QueryParameter> result = new List<QueryParameter>();

			if (!string.IsNullOrEmpty(parameters))
			{
				string[] p = parameters.Split('&');
				foreach (string s in p)
				{
					if (!string.IsNullOrEmpty(s) && !s.StartsWith(OAuthParameterPrefix))
					{
						if (s.IndexOf('=') > -1)
						{
							string[] temp = s.Split('=');
							result.Add(new QueryParameter(temp[0], temp[1]));
						}
						else
						{
							result.Add(new QueryParameter(s, string.Empty));
						}
					}
				}
			}

			return result;
		}

		/// <summary>
		/// This is a different Url Encode implementation since the default .NET one outputs the percent encoding in lower case.
		/// While this is not a problem with the percent encoding spec, it is used in upper case throughout OAuth
		/// </summary>
		/// <param name="value">The value to Url encode</param>
		/// <returns>Returns a Url encoded string</returns>
		private static string UrlEncode(string value)
		{
			StringBuilder result = new StringBuilder();

			foreach (char symbol in value)
			{
				if (unreservedChars.IndexOf(symbol) != -1)
				{
					result.Append(symbol);
				}
				else
				{
					result.Append('%' + String.Format("{0:X2}", (int)symbol));
				}
			}

			return result.ToString();
		}

		/// <summary>
		/// Normalizes the request parameters according to the spec
		/// </summary>
		/// <param name="parameters">The list of parameters already sorted</param>
		/// <returns>a string representing the normalized parameters</returns>
		private static string NormalizeRequestParameters(IList<QueryParameter> parameters)
		{
			StringBuilder sb = new StringBuilder();
			QueryParameter p = null;
			for (int i = 0; i < parameters.Count; i++)
			{
				p = parameters[i];
				sb.AppendFormat("{0}={1}", p.Name, p.Value);

				if (i < parameters.Count - 1)
				{
					sb.Append("&");
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Generate the signature base that is used to produce the signature
		/// </summary>
		/// <param name="url">The full url that needs to be signed including its non OAuth url parameters</param>
		/// <param name="consumerKey">The consumer key</param>        
		/// <param name="token">The token, if available. If not available pass null or an empty string</param>
		/// <param name="tokenSecret">The token secret, if available. If not available pass null or an empty string</param>
		/// <param name="httpMethod">The http method used. Must be a valid HTTP method verb (POST,GET,PUT, etc)</param>
		/// <param name="signatureType">The signature type. To use the default values use <see cref="OAuthBase.SignatureTypes">OAuthBase.SignatureTypes</see>.</param>
		/// <returns>The signature base</returns>
		private static string GenerateSignatureBase(Uri url, string consumerKey, string token, string tokenSecret, string httpMethod, string timeStamp, string nonce, string signatureType, out string normalizedUrl, out string normalizedRequestParameters)
		{
			if (token == null)
			{
				token = string.Empty;
			}

			if (tokenSecret == null)
			{
				tokenSecret = string.Empty;
			}

			if (string.IsNullOrEmpty(consumerKey))
			{
				throw new ArgumentNullException("consumerKey");
			}

			if (string.IsNullOrEmpty(httpMethod))
			{
				throw new ArgumentNullException("httpMethod");
			}

			if (string.IsNullOrEmpty(signatureType))
			{
				throw new ArgumentNullException("signatureType");
			}

			normalizedUrl = null;
			normalizedRequestParameters = null;

			List<QueryParameter> parameters = GetQueryParameters(url.Query);
			parameters.Add(new QueryParameter(OAuthVersionKey, OAuthVersion));
			parameters.Add(new QueryParameter(OAuthNonceKey, nonce));
			parameters.Add(new QueryParameter(OAuthTimestampKey, timeStamp));
			parameters.Add(new QueryParameter(OAuthSignatureMethodKey, signatureType));
			parameters.Add(new QueryParameter(OAuthConsumerKeyKey, consumerKey));

			if (!string.IsNullOrEmpty(token))
			{
				parameters.Add(new QueryParameter(OAuthTokenKey, token));
			}

			parameters.Sort(new QueryParameterComparer());

			normalizedUrl = string.Format("{0}://{1}", url.Scheme, url.Host);
			if (!((url.Scheme == "http" && url.Port == 80) || (url.Scheme == "https" && url.Port == 443)))
			{
				normalizedUrl += ":" + url.Port;
			}
			normalizedUrl += url.AbsolutePath;
			normalizedRequestParameters = NormalizeRequestParameters(parameters);

			StringBuilder signatureBase = new StringBuilder();
			signatureBase.AppendFormat("{0}&", httpMethod.ToUpper());
			signatureBase.AppendFormat("{0}&", UrlEncode(normalizedUrl));
			signatureBase.AppendFormat("{0}", UrlEncode(normalizedRequestParameters));

			return signatureBase.ToString();
		}

		/// <summary>
		/// Generate the signature value based on the given signature base and hash algorithm
		/// </summary>
		/// <param name="signatureBase">The signature based as produced by the GenerateSignatureBase method or by any other means</param>
		/// <param name="hash">The hash algorithm used to perform the hashing. If the hashing algorithm requires initialization or a key it should be set prior to calling this method</param>
		/// <returns>A base64 string of the hash value</returns>
		private static string GenerateSignatureUsingHash(string signatureBase, HashAlgorithm hash)
		{
			return ComputeHash(hash, signatureBase);
		}

		/// <summary>
		/// Generates a signature using the specified signatureType 
		/// </summary>		
		/// <param name="url">The full url that needs to be signed including its non OAuth url parameters</param>
		/// <param name="consumerKey">The consumer key</param>
		/// <param name="consumerSecret">The consumer seceret</param>
		/// <param name="token">The token, if available. If not available pass null or an empty string</param>
		/// <param name="tokenSecret">The token secret, if available. If not available pass null or an empty string</param>
		/// <param name="httpMethod">The http method used. Must be a valid HTTP method verb (POST,GET,PUT, etc)</param>
		/// <param name="signatureType">The type of signature to use</param>
		/// <returns>A base64 string of the hash value</returns>
		public static string GenerateSignature(Uri url, string consumerKey, string consumerSecret, string token, string tokenSecret, string httpMethod, SignatureTypes signatureType, string time, string nonce, out string normalizedUrl, out string normalizedRequestParameters)
		{
			normalizedUrl = null;
			normalizedRequestParameters = null;

			switch (signatureType)
			{
				case SignatureTypes.PLAINTEXT:
					return HttpUtility.UrlEncode(string.Format("{0}&{1}", consumerSecret, tokenSecret));
				case SignatureTypes.HMACSHA1:
					string sha1base = GenerateSignatureBase(url, consumerKey, token, tokenSecret, httpMethod, time, nonce, "HMAC-SHA1", out normalizedUrl, out normalizedRequestParameters);
					HMACSHA1 hmacsha1 = new HMACSHA1();
					hmacsha1.Key = Encoding.ASCII.GetBytes(string.Format("{0}&{1}", UrlEncode(consumerSecret), string.IsNullOrEmpty(tokenSecret) ? "" : UrlEncode(tokenSecret)));
					return GenerateSignatureUsingHash(sha1base, hmacsha1);
				case SignatureTypes.HMACSHA256:
					string sha256base = GenerateSignatureBase(url, consumerKey, token, tokenSecret, httpMethod, time, nonce, "HMAC-SHA256", out normalizedUrl, out normalizedRequestParameters);
					HMACSHA256 hmacsha256 = new HMACSHA256();
					hmacsha256.Key = Encoding.ASCII.GetBytes(string.Format("{0}&{1}", UrlEncode(consumerSecret), string.IsNullOrEmpty(tokenSecret) ? "" : UrlEncode(tokenSecret)));

					return GenerateSignatureUsingHash(sha256base, hmacsha256);
				default:
					throw new ArgumentException("Unknown signature type", "signatureType");
			}
		}

		public static string GenerateAuthorizationHeaderValue(Uri uri, string consumerKey, string consumerSecret, string token, string tokenSecret, string httpMethod, string realm, SignatureTypes signatureMethod = SignatureTypes.HMACSHA256)
		{
			string time = GenerateTimeStamp();
			string nonce = GenerateNonce();

			string signature = GenerateSignature(uri, consumerKey, consumerSecret, token, tokenSecret, httpMethod, signatureMethod, time, nonce, out _, out _);

			if (signature.Contains("+"))
			{
				signature = signature.Replace("+", "%2B");
			}

			string header = "OAuth ";
			header += "oauth_signature=\"" + signature + "\",";
			header += "oauth_version=\"1.0\",";
			header += "oauth_nonce=\"" + nonce + "\",";
			header += "oauth_signature_method=\"" + SignatureTypeNames.GetValueOrDefault(signatureMethod) + "\",";
			header += "oauth_consumer_key=\"" + consumerKey + "\",";
			header += "oauth_token=\"" + token + "\",";
			header += "oauth_timestamp=\"" + time + "\",";
			header += "realm=\"" + realm + "\"";

			return header;
		}

		/// <summary>
		/// Generate the timestamp for the signature        
		/// </summary>
		/// <returns></returns>
		private static string GenerateTimeStamp()
		{
			// Default implementation of UNIX time of the current UTC time
			TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return Convert.ToInt64(ts.TotalSeconds).ToString();
		}

		/// <summary>
		/// Generate a nonce
		/// </summary>
		/// <returns></returns>
		private static string GenerateNonce()
		{
			var random = new Random();
			// Just a simple implementation of a random number between 123400 and 9999999
			return random.Next(123400, 9999999).ToString();
		}

	}
}
