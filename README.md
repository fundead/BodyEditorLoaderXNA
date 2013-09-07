BodyEditorLoaderXNA
===================

A port of Body Editor Loader to .NET, for loading fixtures created with Physics Body Editor into XNA projects that use Farseer.

About
------

This project exposes an API for loading bodies created with [Physics Body Editor](http://www.aurelienribon.com/blog/projects/physics-body-editor/) into XNA games, ready for use in a Farseer physics world. It is a direct port of the [Body Editor Loader](https://code.google.com/p/box2d-editor/source/browse/loader-libgdx/src/aurelienribon/bodyeditor/BodyEditorLoader.java?r=6a4430d04ece96a2b22dfde528531fa600776519) from Java using Box2D to .NET/C#/XNA using Farseer. It allows you to instantiate Farseer.Fixtures automatically from the JSON payload Physics Body Editor creates using its GUI.
It is confirmed to build with .NET 4.5, and probably earlier (client profile too, barring misfortune).

Usage
------

This assumes you have an XNA project with the Farseer library, and a Farseer.World to add the fixtures to.

1. Clone this repo into your solution; in VS add BodyEditorLoad.csproj as a project to the solution.
2. Optional but advised: remove the FarseerPhysics class library provided and replace it with a reference to your own (in your main project).
3. Add BodyEditorLoader as a reference to your main project.
4. Create your fixtures and bodies in Physics Body Editor, save the resulting JSON & copy it to your VS solution.
5. In a class where you want to do the loading for your fixtures, create a new instance of BEL, passing it your Farseer physics world, and let it read in your JSON data:

```
DirectoryInfo filepath = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "/level1bodies.json");
var platformsLoader = new BodyEditorLoader(filepath.FullName);
            
var _myBody = new Body(_myFarseerPhysicsWorld);
bodyLoader.attachFixture(_myBody, "MyExampleBody", 3000f);
```

The second parameter in attachFixture() is the name of the body to load, and will be the name you it the body in Physics Body Editor. The third is the scale factor, and will need to be tweaked based on your physics units and rendering scaling, etc.

Ported by
---------

Fundead, at [http://pixelpegasus.cloudns.org](http://pixelpegasus.cloudns.org)

License
--------

MIT license
