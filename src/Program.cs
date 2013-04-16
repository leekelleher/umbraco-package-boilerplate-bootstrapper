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
			//
			// TODO: [LK] Make these consts configurable (via CLI args)
			//

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
			var csprojGuid = Guid.NewGuid().ToString("B").ToUpper();
			var solutionFile = Path.Combine(targetFolder, "src", "Our.Umbraco.Package.sln");
			if (File.Exists(solutionFile))
			{
				var sln = File.ReadAllText(solutionFile);
				sln = sln.Replace("{C0A8BDDE-A89F-4341-8912-A7C05E5CC25F}", Guid.NewGuid().ToString("B").ToUpper());
				sln = sln.Replace("{0F945291-F264-44AF-9C6A-B1BB0B9E785E}", Guid.NewGuid().ToString("B").ToUpper());
				sln = sln.Replace("{04F1C33F-EF3D-4F85-90B6-D258F06752D7}", Guid.NewGuid().ToString("B").ToUpper());
				sln = sln.Replace("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", Guid.NewGuid().ToString("B").ToUpper());
				sln = sln.Replace("{E588D5C0-D4C4-4CD2-9416-3D8C479CBA06}", csprojGuid);
				sln = sln.Replace("Our.Umbraco.Package", projectNamespace);
				File.WriteAllText(solutionFile, sln);

				File.Move(solutionFile, Path.Combine(targetFolder, "src", string.Concat(projectNamespace, ".sln")));
				Console.WriteLine("Updated the solution file.");
			}

			// csproj
			var csprojFolder = Path.Combine(targetFolder, "src", "Our.Umbraco.Package");
			if (Directory.Exists(csprojFolder))
			{
				var csprojFile = Path.Combine(csprojFolder, "Our.Umbraco.Package.csproj");
				if (File.Exists(csprojFile))
				{
					var csproj = File.ReadAllText(csprojFile);
					csproj = csproj.Replace("{E588D5C0-D4C4-4CD2-9416-3D8C479CBA06}", csprojGuid);
					csproj = csproj.Replace("Our.Umbraco.Package", projectNamespace);
					File.WriteAllText(csprojFile, csproj);

					var assemblyInfoFile = Path.Combine(csprojFolder, "Properties", "AssemblyInfo.cs");
					if (File.Exists(assemblyInfoFile))
					{
						var assemblyInfo = File.ReadAllText(assemblyInfoFile);
						assemblyInfo = assemblyInfo.Replace("Our Umbraco Package", projectName);
						assemblyInfo = assemblyInfo.Replace("Our.Umbraco.Package", projectNamespace);
						assemblyInfo = assemblyInfo.Replace("2013", DateTime.Today.Year.ToString());
						assemblyInfo = assemblyInfo.Replace("87f49b1c-c55e-4e77-9771-0e40dd287006", Guid.NewGuid().ToString("D").ToUpper());
						File.WriteAllText(assemblyInfoFile, assemblyInfo);
						Console.WriteLine("Updated: {0}", Path.GetFileName(assemblyInfoFile));
					}

					File.Move(csprojFile, Path.Combine(csprojFolder, string.Concat(projectNamespace, ".csproj")));
					Directory.Move(csprojFolder, Path.Combine(targetFolder, "src", projectNamespace));
					Console.WriteLine("Updated the project file.");
				}
			}

			// package.xml
			var packageXml = Path.Combine(targetFolder, "package", "package.xml");
			if (File.Exists(packageXml))
			{
				var xml = File.ReadAllText(packageXml);
				xml = xml.Replace("<name>Package Name</name>", string.Concat("<name>", projectName, "</name>"));
				File.WriteAllText(packageXml, xml);
				Console.WriteLine("Updated: {0}", Path.GetFileName(packageXml));
			}

			// package.build.xml
			var buildXml = Path.Combine(targetFolder, "package", "package.proj");
			if (File.Exists(buildXml))
			{
				var xml = File.ReadAllText(buildXml);
				xml = xml.Replace("Our.Umbraco.Package", projectNamespace);
				xml = xml.Replace("StartDate=\"2012-09-10\"", string.Format("StartDate=\"{0:yyyy-MM-dd}\"", DateTime.Today));
				File.WriteAllText(buildXml, xml);
				Console.WriteLine("Updated: {0}", Path.GetFileName(buildXml));
			}

			// update the ignore rules in .gitignore and .hgignore
			var ignoreFiles = new[] { ".gitignore", ".hgignore" };
			foreach (var ignoreFile in ignoreFiles)
			{
				var ignorePath = Path.Combine(targetFolder, ignoreFile);
				if (File.Exists(ignorePath))
				{
					var rules = File.ReadAllText(ignorePath);
					rules = rules.Replace("Package_Name_*.zip", string.Concat(projectName.Replace(" ", "_"), "_*.zip"));
					File.WriteAllText(ignorePath, rules);
					Console.WriteLine("Updated: {0}", Path.GetFileName(ignorePath));
				}
			}

			// files to be removed
			var removals = new[] { "docs\\.gitignore", "lib\\.gitignore" };
			foreach (var removal in removals)
			{
				var removalPath = Path.Combine(targetFolder, removal);
				if (File.Exists(removalPath))
				{
					File.Delete(removalPath);
					Console.WriteLine("Removed: {0}", Path.GetFileName(removalPath));
				}
			}
			Console.WriteLine("Package boilerplate tidy-up complete.");


			// Ta-dah
			Console.WriteLine("All done! Go forth an build an Umbraco package!");
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
