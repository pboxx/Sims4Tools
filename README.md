s4pe, based on the s4pi interface library, is an editing tool for the .package format used with [The Sims 4]. This is an open source project based on [s3pi and s3pe]. Documentation on s3pi ([Sims 3 Package Interface]) can be found [here] (http://s3pi.sourceforge.net/); a lot of this is still relevant. 


Current Version
----

[0.6.0] is the most recent version. The latest code can be found in the [develop branch] (https://github.com/s4ptacle/Sims4Tools/tree/develop).

The [repository we forked from] is not being maintained any more. 


Contributors
-----------

Roughly in chronological order -- [full details here] (https://github.com/s4ptacle/Sims4Tools/graphs/contributors)

* Peter Jones - Main author of s3pe/s3pi
* [Rick] - a pioneer in TS4
* [ChaosMageX] - Initial s4p* setup; work on DATA, RIG and GEOM wrappers
* [andrewtavera] - Mesh parts and other help
* [granthes] - Several contributions pre-release and in the early stages
* [snaitf] - Decoding and contributions for CCOL, COBJ, trims as well as bugfixes
* [IngeJones] - a kind lady who doesn't want her name mentioned
* [Kuree] - Maintained the project in 2014 and 2015
* [CmarNYC] - current contributions see [here] (https://github.com/s4ptacle/Sims4Tools/commits/develop?author=cmarNYC)
* [pbox] - current contributions see [here] (https://github.com/s4ptacle/Sims4Tools/commits/develop?author=pboxx)
* [Buzzler] - current contributions see [here] (https://github.com/s4ptacle/Sims4Tools/commits/develop?author=BrutalBuzzler)

Requirements
-------------
* .NET 4.0

Project Setup
-------------
* Visual Studio is currently required. sims4tools.sln is the solution currently in use.

Helper Projects
-------------

Simplified overview how to make a helper tool using s4pi

**Overview**

* A helper is a standalone tool that can open resources as they are exported from a package - long cryptic names and all. At minimum it must be able to be opened by being called from another application (e.g. s4pe) with the filename of the resource as a parameter.  But you can enhance your tool if you wish, so that it can be opened in other ways and used for other roles.
* The interface between s4pe and your app is a .helper text file, which you edit to call your app in the appropriate way and provide with your tool.  The user may have to edit that if they install somewhere you did not forsee.
* The helper should include all library dlls it relies on. It should not rely on finding the dlls that are installed with the user's copy of s4pe. This is so that if the user has an older or newer version of s4pe than the one you had when you wrote your tool, they won't end up with compatibility problems trying to use your tool.  Your tool needs to have access to the version of the dlls YOU compiled it with.
* A helper app can be installed in two different ways.  It can either be totally outside the s4pe folder like any external program, or it can live under the /Helpers folder in s4pe, inside its own subfolder along with its own dlls, with only its .helper textfile directly under /Helpers

**The difference in concept between a helper and a wrapper**

A wrapper should be as dumb as possible and not hide or rearrange any of the data in a resource, even if we do not know what it represents, or if it is stored in a strange order.  A helper on the other hand can display the resource contents in a user-friendly way, prettied up, and bits hidden if they might confuse the user.

**How to make**

*  Download the latest s4pi library.  Unpack into folder somewhere your Visual Studio can access.
*  Begin your C# solution
*  As you need to use any s3pi dll, add it to your solution references.  This will cause it to be added to your solution's bin folder. 
*  Any s4pi dlls in your bin/Release folder should be included with what you publish.  You should not design your tool so it has to use the s4pi dlls in your user's s3pe folder.   
*  Support for writing your .helper file can be found in your installed s4pe/Helpfiles folder, called Helpers.txt

(Peter Jones, updated Jan 15th 2012 by Inge)

-------------
* There is a separate (old) repo for Helper projects, [s4pe-helper]. However, this looks just as abandoned as Kuree's main repo.

How to Contribute
-----------
* Fork the project, modify it, and send us a pull request!

License
----
[GNU General Public License v3] 


Other
----
#### Special thanks:
Without Peter's work on s3pe/s3pi, this project would not exist. His philosophy to share and distribute this as an open source project will be carried on.

[s3pi and s3pe]: http://sourceforge.net/projects/sims3tools/
[Sims 3 Package Interface]: https://sourceforge.net/projects/s3pi/

[Kuree]:https://github.com/Kuree
[ChaosMageX]:https://github.com/ChaosMageX
[andrewtavera]:https://github.com/andrewtavera
[IngeJones]:https://github.com/IngeJones
[Rick]:https://gib.me
[granthes]:https://github.com/granthes
[snaitf]: https://github.com/Snaitf
[s4pe-helper]: https://github.com/Kuree/s4p4-helper
[wiki]:https://github.com/Kuree/s4p4-helper/wiki
[Buzzler]:https://github.com/BrutalBuzzler
[CmarNYC]:https://github.com/cmarNYC
[pbox]:https://github.com/pboxx
[repository we forked from]:https://github.com/Kuree/Sims4Tools
[GNU General Public License v3]:http://www.gnu.org/licenses/gpl-3.0.html
[The Sims 4]:https://en.wikipedia.org/wiki/The_Sims_4
[0.4.3]:https://github.com/s4ptacle/Sims4Tools/releases/tag/0.4.3-release
[0.4.4-beta]:https://github.com/s4ptacle/Sims4Tools/releases/tag/0.4.4-beta
