# Contributing to DOML.net

This document just layouts some specifications for contributing to this repository.

## Issues/PRs

Each PR should have a single purpose, i.e. fix a bug, or implement a single feature.  Some bugs may be grouped together if related but really each bug should have its own PR.  Opening an issue before each PR is standard procedure.

Discussion about features should exist within the relevant issue (if there isn't one open a new one), rather than the PR itself; discussion about the implementation of the feature is for the PR.  Furthermore discussion about the feature in relation to the DOML spec i.e. disagreement about syntax should occur at the DOML repository rather then here, you can find it [here](https://github.com/DOML-Lang/DOML).

## Style

We follow the standard DOML style guide as detailed [here](https://github.com/DOML-Lang/DOML/blob/master/doml_styleguide.md).

We have standardized on Microsoft's [C# Coding Conventions](https://msdn.microsoft.com/en-us/library/ff926074.aspx) and [General Naming Conventions](https://msdn.microsoft.com/en-us/library/ms229045(v=vs.110).aspx).  However we put braces on the same line as a compact style.

As a TL;DR on our coding practices, adhere to the following example:

```c#
// All files begin with the following license header (remove this line):
#region License
// ====================================================
// DOML Lang Copyright(C) 2018 DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System; // System usings go first
using DOML.AST; // using's are sorted alphabetically
using DOML.IR;

// Use camelCasing unless stated otherwise.
// Descriptive names for variables/methods should be used.
// Fields, properties and methods should always specify their scope, aka private/protected/internal/public.

// Interfaces start with an I and should use PascalCasing.
public interface IInterfaceable {
}

// Class names should use PascalCasing.
// Braces are on the same line

/// <summary>
/// Xml documentation comments are encouraged. Describe public APIs and the intent of code, not implementation details.
/// </summary>
public class Class {
    // Private fields should be camelCased.
    // Use properties for any field that needs access levels other than private
    private string someField;

    // Events should use PascalCasing as well.
    // ✓ DO name events with a verb or a verb phrase.
    // Examples include Clicked, Painting, DroppedDown, and so on.
    // ✓ DO give events names with a concept of before and after, using the present and past tenses.
    // For example, a close event that is raised before a window is closed would be called Closing,
    // and one that is raised after the window is closed would be called Closed.
    public event EventHandler<EventArgs> SomeEvent;

    // Properties should use PascalCasing.
    public int MemberProperty { get; set; }

    // Methods should use PascalCasing.
    // Method parameters should be camelCased.
    public void SomeMethod(int functionParameter) {
        // Local variables should also be camelCased.
        int myLocalVariable = 0;
        
        // Compact brace style
        if (x) {
            // ..
        } else if (y) {
            // ..
        } else {
           // ..
        }
    }
}
```

The only thing I will note: is to avoid the use of `var` as it often makes statements harder to read, for example the following should be avoided;
```C#
// No
var x = 5;
// Okay but would prefer `Dictionary<string, int> dict = new();` when that syntax is official
var dict = new Dictionary<string, int>();
// No
foreach (var node in nodes) { ... }
```
While stylecop will allow it anywhere (except for cases where you can't see the type) I'll flag any cases where you do the first one, and may flag the second if it makes it harder to read.  I'll try not to be too pedantic though :).

I'm quite happy to change the style guide for this project if enough people have valid criticisms :) and not just 'I prefer it this way'.

It is also highly recommended that you install [StyleCop](https://github.com/TeamPorcupine/ProjectPorcupine/wiki/StyleCop), which will automatically point out any deviations from the project's style guidelines. Any deviations in your code which can be tracked by StyleCop will result in the rejection of your Pull Request.

## SourceMonitor

I use source monitor to long term track the effects of changes and as a way to measure refactors, you shouldn't add any new checkpoints to the commit as I'll do those after refactors or prior to releases.  There are versions which you can use wine with (and even a version that doesn't require wine) to use it on Mac (which I use when developing on mac), you can also just generally check your code with any complexity checker as that is the main metric (and while we do use the modified variant for source monitor it is pretty close to the normal metric).
