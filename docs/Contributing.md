# Contributing

This file lists the contributing guidelines that are used in the project.

### Commit style guide

Commits start with a capital letter and don't end in a punctuation mark.

Right:
```
Treat usernames as case-insensitive in user collections
```

Wrong:
```
treat usernames as case-insensitive in user collections.
```

Use imperative present tense in commit messages instead of past tense.

Right:
```
Add null-check for GameMode
```

Wrong:
```
Added null-check for GameMode
```

### Game Support

When writing code or planning a feature, consider that WAE supports a broad number of games: Tiberian Sun and Red Alert 2 and all of their mods. Try to make features beneficial to all games as far as possible.

WAE supports all target games through a single executable. All submitted code _must_ be compatible with all games. If a specific feature is not available in a game, make it so the feature can be disabled in the editor's configuration for the game.

There are 3 branches: `master` is for Dawn of the Tiberium Age, `tsclient` is for Tiberian Sun (CnCNet Client version), and `yr` is for Yuri's Revenge. Code-wise these branches are identical, but `tsclient` and `yr` have one additional commit that gives them different INI configurations compared to `master`.

### Translations

If your code introduces new strings that are displayed in the user interface, the strings _must_ go through the fitting `Translate` function to enable multi-language support. The codebase has a lot of examples on how to do this. Also, you need to add the new strings in a fitting location in the `Translation_en.ini` reference translation file.

Strings that are generally not meant to be displayed in the user interface (such as logging calls and thrown exceptions) should not be translated, and should always be written in English.

If your code significantly modifies existing strings so that their meaning is changed (fixing typos etc. does not count), you need to update the translation key for that string to signal the change to translators. A good way to do this is to append an incrementing version identifier to the translation key, or increment the key if it already exists. For example, if you modified the `ExpandMapWindow.InvalidWidth.Title` string in a significant way, you'd change its translation key to `ExpandMapWindow.InvalidWidth.Title.v2`. If then, you returned and changed its meaning again at a later date, you'd change the translation key to `ExpandMapWindow.InvalidWidth.Title.v3`.

### Pull Requests

Make sure that the scope of your pull request is well defined. Pull requests can take significant developer time to review and very large pull requests or pull requests with poorly defined scope can be difficult to review.

One pull request should _only implement one feature_ or _fix one bug_, unless there is a good reason for grouping the changes together.

Do not heavily refactor the style of existing code in a pull request, unless the refactored code fits to the scope of the pull request (feature or bug fix). Rather, if you want to refactor existing code just for the sake of refactoring or getting rid of technical debt, create a secondary pull request for that purpose.

**Make sure your code and commits match this style guide before you create your pull request.**

Pull requests that are not well defined in their scope or pull requests that don't otherwise match this guide can end up rejected and closed by the staff.

### Code style guide

We have established a couple of code style rules to keep things consistent. Please check your code style before committing the code.
- We use spaces instead of tabs to indent code.
- Curly braces are always to be placed on a new line. One of the reasons for this is to clearly separate the end of the code block head and body in case of multiline bodies:
```cs
if (SomeReallyLongCondition() ||
    ThatSplitsIntoMultipleLines())
{
    DoSomethingHere();
    DoSomethingMore();
}
```
- Braceless code block bodies should be made only when both code block head and body are single line. Statements that split into multiple lines and nested braceless blocks are not allowed within braceless blocks:
```cs
// OK
if (Something())
    DoSomething();

// OK
if (SomeReallyLongCondition() ||
    ThatSplitsIntoMultipleLines())
{
    DoSomething();
}

// OK
if (SomeCondition())
{
    if (SomeOtherCondition())
        DoSomething();
}

// OK
if (SomeCondition())
{
    return VeryLongExpression()
        || ThatSplitsIntoMultipleLines();
}
```
- Only empty curly brace blocks may be left on the same line for both opening and closing braces (if appropriate).
- If you use `if`-`else` you should either have all of the code blocks braced or braceless to keep things consistent.
- Code should have empty lines to make it easier to read. Use an empty line to split code into logical parts. It's mandatory to have empty lines to separate:
  - `return` statements (except when there is only one line of code except that statement);
  - local variable assignments that are used in the further code (you shouldn't put an empty line after one-line local variable assignments that are used only in the following code block though);
  - code blocks (braceless or not) or anything using code blocks (function or hook definitions, classes, namespaces etc.)
```cs
// OK
int localVar = Something();
if (SomeConditionUsing(localVar))
    ...

// OK
int localVar = Something();
int anotherLocalVar = OtherSomething();

if (SomeConditionUsing(localVar, anotherLocalVar))
    ...

// OK
int localVar = Something();

if (SomeConditionUsing(localVar))
    ...

if (SomeOtherConditionUsing(localVar))
    ...

localVar = OtherSomething();

// OK
if (SomeCondition())
{
    Code();
    OtherCode();

    return;
}

// OK
if (SomeCondition())
{
    SmallCode();
    return;
}
```
- Use `var` with local variables when the type of the variable is obvious from the code or the type is not relevant. Never use `var` with primitive types.
```cs
// OK
var list = new List<int>();

// Not OK
var something = 6;
```
- A space must be put between braces of empty curly brace blocks.
- Local variables, function/method args and private class fields are named in `camelCase` and a descriptive name, like `houseType` for a local `HouseType` variable.
- Classes, namespaces, and properties are always written in `PascalCase`.
- Class fields that can be set via INI tags should be named exactly like the related INI tags.

Note: The style guide is not exhaustive and may be adjusted in the future.
