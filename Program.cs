using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace UpdatePfSenseCerts
{
	class Config
	{
		public string PfApiKey { get; set; }
		public string PfApiSecret { get; set; }
		public string PfUrl { get; set; }
		public string CertFile { get; set; }
		public string KeyFile { get; set; }
	}

	static class Program
	{
		static Config config;

		static JObject PfApiCall(string query, string postData = null)
		{
			var timestamp = DateTime.UtcNow.ToString("yyyyMMddZHHmmss");
			var nonce = Util.Random(4).ToHexString();
			var hash = Util.Sha256($"{config.PfApiSecret}{timestamp}{nonce}".GetBytes()).ToHexString();
			var auth = $"{config.PfApiKey}:{timestamp}:{nonce}:{hash}";

			using var http = new WebClient();
			var url = $"{config.PfUrl}/fauxapi/v1/?{query}";
			http.Headers.Add("fauxapi-auth", auth);
			var response = postData != null ? http.UploadString(url, postData) : http.DownloadString(url);

			var json = JObject.Parse(response);

			return json;
		}

		static void Main(string[] args)
		{
			var exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
			var configPath =
				args.Length >= 1 ? args[0] :
				Path.Combine(Path.GetDirectoryName(exePath), "config.json");
			config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));

			Console.WriteLine($"Updating SSL certificates on '{config.PfUrl}'.");
			Console.WriteLine($"Cert file: {config.CertFile}");
			Console.WriteLine($"Key file: {config.KeyFile}");

			ServicePointManager.ServerCertificateValidationCallback =
				(sender, certificate, chain, sslPolicyErrors) => true;

			var certFile = File.ReadAllBytes(config.CertFile);
			var keyFile = File.ReadAllBytes(config.KeyFile);

			var configGet = PfApiCall("action=config_get");

			Console.WriteLine($"Got config. ({configGet["action"]}:{configGet["message"]})");

			var certRef = (string)configGet["data"]["config"]["system"]["webgui"]["ssl-certref"];
			var cert = configGet["data"]["config"]["cert"].First(cert => (string)cert["refid"] == certRef);
			cert["crt"] = certFile.GetBase64();
			cert["prv"] = keyFile.GetBase64();
			var certConfig = "{" + cert.Parent.Parent.ToString() + "}";

			var updateResult = PfApiCall("action=config_patch", certConfig);

			Console.WriteLine($"Updated config. ({updateResult["action"]}:{updateResult["message"]})");

			var restartResult = PfApiCall("action=send_event", "[\"service restart webgui\"]");

			Console.WriteLine($"Restarted web GUI. ({restartResult["action"]}:{restartResult["message"]})");
			Console.WriteLine($"Done.");
		}
	}
}
