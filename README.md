# Umbraco Package Boilerplate Bootstrapper

A command-line bootstrapper for [Umbraco Package Boilerplate](https://github.com/leekelleher/umbraco-package-boilerplate).

## Build

At present, to build the `upbb.exe` bootstrapper, you will need to download the source-code from this repository and run the `Build.cmd` script. This will compile the code and put the executable in the `/build` directory.

Once you have the `upbb.exe`, you can XCOPY it wherever you like. For global access to the bootstrapper, you can add the target directory to your `%PATH%` environment variable.

In future there will be a downloadable executable, but currently the code-base is very much still a work-in-progress.

## Usage

At a MS-DOS command-line, call the `upbb.exe` along with the name of your new package, like so:

	upbb <package name>

The bootstrapper will scaffold out the boilerplate structure for your new Umbraco package.

## Contributions

This project is open to community contributions and collaboration. Feel free to fork the code-base, submit a pull request and/or suggest features in the issue tracker.

## References

* [Umbraco Package Boilerplate](https://github.com/leekelleher/umbraco-package-boilerplate)
* [Matt Brailsford](https://github.com/mattbrailsford)'s blog post: [Automating Umbraco Package Creation Using MSBuild](http://blog.mattbrailsford.com/2010/11/13/automating-umbraco-package-creation-using-msbuild/)
