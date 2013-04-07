using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Our.Umbraco.Package.Boilerplate.Bootstrapper
{
	class Program
	{
		static void Main(string[] args)
		{
			// TODO: [LK] Make these consts configurable (via CLI args)
			const string github = "https://github.com";
			const string username = "leekelleher";
			const string repo = "umbraco-package-boilerplate";
			const string branch = "master";

			var downloadUrl = string.Concat(github, "/", username, "/", repo, "/archive/", branch, ".zip");
			var projectName = string.Empty;

			// read args - check for missing params
			if (args.Length >= 1)
				projectName = args[0];

			if (string.IsNullOrWhiteSpace(projectName))
			{
				// prompt for project-name
				Console.Write("Please enter the name of your project: ");
				projectName = Console.ReadLine();
			}

			if (string.IsNullOrWhiteSpace(projectName))
				return;

			// sanitise string
			var projectNamespace = SanitiseInput(projectName);
			Console.WriteLine("Project name: {0}", projectName);
			Console.WriteLine("Project namespace: {0}", projectNamespace);

			// download the zip from github
			var currentDirectory = Directory.GetCurrentDirectory();
			var tmpZip = Path.Combine(currentDirectory, string.Concat(repo, ".zip"));
			if (!File.Exists(tmpZip))
			{
				using (var client = new WebClient())
				{
					Console.Write("Downloading package boilerplate.");
					// TOOD: [LK] Use aync? Display the percentage of download?
					client.DownloadFile(new Uri(downloadUrl), tmpZip);
					Console.WriteLine(" Done.");
				}
			}

			if (!File.Exists(tmpZip))
				return;

			var sourceFolder = Path.Combine(currentDirectory, string.Concat(repo, "-", branch));
			var targetFolder = Path.Combine(currentDirectory, projectNamespace);

			// extract into folder
			if (!Directory.Exists(sourceFolder))
			{
				using (var zip = new Internals.Unzip(tmpZip))
				{
					zip.ExtractToDirectory(currentDirectory);
					Console.WriteLine("Package boilerplate extracted.");
				}

				// delete the temp zip file.
				File.Delete(tmpZip);

				// rename the source folder (e.g. root folder from github's download)
				if (!Directory.Exists(targetFolder))
				{
					Directory.Move(sourceFolder, targetFolder);
					Console.WriteLine("Package boilerplate directory renamed.");
				}
			}

			if (!Directory.Exists(targetFolder))
				return;

			//
			// find-n-replace the tokens
			//

			// visual studio solution file
			var solutionFile = Path.Combine(targetFolder, "Our.Umbraco.Package.sln");
			if (File.Exists(solutionFile))
			{
				File.Move(solutionFile, Path.Combine(targetFolder, string.Concat(projectNamespace, ".sln")));
				Console.WriteLine("Updated the project solution file.");
			}

			// package.xml
			var packageXml = Path.Combine(targetFolder, "Build", "package.xml");
			if (File.Exists(packageXml))
			{
				var xml = File.ReadAllText(packageXml);
				xml = xml.Replace("<name>Package Name</name>", string.Concat("<name>", projectName, "</name>"));
				File.WriteAllText(packageXml, xml);
				Console.WriteLine("Updated: {0}", Path.GetFileName(packageXml));
			}

			// package.build.xml
			var buildXml = Path.Combine(targetFolder, "Build", "package.build.xml");
			if (File.Exists(buildXml))
			{
				var xml = File.ReadAllText(buildXml);
				xml = xml.Replace("Our.Umbraco.Package", projectNamespace);
				xml = xml.Replace("StartDate=\"10/09/2012\"", string.Format("StartDate=\"{0:dd/MM/yyyy}\"", DateTime.Today));
				File.WriteAllText(buildXml, xml);
				Console.WriteLine("Updated: {0}", Path.GetFileName(buildXml));
			}

			// ta-dah
			Console.WriteLine("All done! Go forth an build a package for Umbraco!");
			Console.ReadLine();
		}

		private static string SanitiseInput(string input)
		{
			var output = input;

			// remove invalid chars
			var invalidChars = string.Concat(new string(Path.GetInvalidFileNameChars()), new string(Path.GetInvalidPathChars()));
			var r = new Regex(string.Format("[{0}]", Regex.Escape(invalidChars)));
			output = r.Replace(input, string.Empty);

			// swap out spaces for dots
			output = output.Replace(" ", ".");

			return output;
		}
	}
}
