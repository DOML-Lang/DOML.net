# Data Oriented Markup Language - DOML (.Net)
This is the .net implementation for DOML (Data Oriented Markup Language), which is a 'new' markup language that takes a different approach then most.  It enacts to simulate a call-stack rather than simulate data structures, this allows it to represent a constructor like look rather than the usual `{...}` mess.

*Note* Currently I've made the following headers equivalent to the spec ones but overtime they should become more distinct.

## An example
** Note: Code Alignment is especially tricky with Github READMEs so if the code isn't always aligned then excuse that please, I will fix it**
``` C
// This is a test for ssm syntax highlighting
@ Test        = System.Color ...
;             .RGB             = 255, 64, 128

@ TheSame     = System.Color ...
;             .RGB(Normalised) = 1, 0.25, 0.5 // 'ish' normally I would round down not up but eh

@ AgainSame   = System.Color ...
;             .RGB(Hex)        = 0xFF4080
;             .Name            = "OtherName

/* Multi Line blocks are great
  /* Especially when nesting is allowed */
  
  Anyways lets go and copy another previous one by just copying over the values.
*/
@ Copy        = System.Color ...
;             .RGB             = Test.RGB
;             .Name            = "Copy"
```
Now while this may seem like a normal programming language, I'm sure you would be quite surprised to hear that in actual fact it is anything but an actual programming language, and as stated before is rather a markup language.  A lot is stripped from programming languages but the core look and feel, to maintain an ease of accessibility.

Compared to perhaps an equivalent JSON;
```JSON
[ 
  {
    "Name": "Test" // We need this cause we will copy it later, meaning by not adding this we will also have a bug
    "Red": 255,
    "Green": 64,
    "Blue": 128
  },
  {
    "Normalised_Red": 1,     // You would have to handle this somehow
    "Normalised_Green": 0.25,
    "Normalised_Blue": 0.5
  },
  {
    "Color_Hex" : "FF4080", // Note: that as a user you would have to perform the conversion (and check if its valid hex)
    "Name": "OtherName"
  },
  {
    "Name": "Copy",
    "Copying": "Test" // And now we enter into string definitions which commonly lead to bugs (i.e. typos)
  }
]
```
Now I won't go into the obfuscation of the syntax of JSON, or how annoying it is to handle cases where you can have different 'inputs' (and that is why its often not done), or how many bugs could exist simply by typos or similar that wouldn't be caught till the execution of the logic (compared to the 'compilation' of CSML code picking up the majority of all errors/typos).

I'll break down the simple syntax later, but do note that there are 0 keywords, and only a few operators (i.e. `;`, `@`, `.`, `=`, `,`, `...` and brackets/braces/arrows/parentheses) this means that there is NO `+`, `-` or any mathematical operators there are no expressions to parse that are any more complicated then what is above.  

## Is it turing complete though?
It's a markup language why would it be? *ruffled mumbling* (Wait it is?  Why is it?).  Yes its turing complete despite a lack of expression parsing, this is mainly due to the fact it effectively operators on a stack of commands and since you can refer to previous commands you could have a source of addition, though arguably while its easier to implement (by a landslide) then JSON it still doesn't contain control blocks/loops (though I don't see how someone couldn't add that similar to how maths is implemented below).  NOTE: all this is implemented on 'your' side aka, similar to how JSON and XML have a deserialization set of procedures so will CSML that will allow you to interface with it (again I'll cover it later).

## Why no expression parsing
The reason is simple; expression parsing complicates the procedure to parse any language and the removal of the need for complicated parsing greatly boosts the speed of parsing and requires you to think in terms of objects rather than in terms of a language.  This is of course not actually entirely true you could of course build a library that supplies math like commands and so something like;
```C
@ Result    = Math.Add ...
;           .Values = 1, 2.02, -5, -9.04

@ PrintOut  = Logger.Log ...
;           .ToLog = Result.AsInt, Result.AsFloat, Result.AsDouble, Result.AsUnsignedInt
```
Now while this isn't implemented by default the actual implementation details would be trivial.  Thus I guess in a way you could refer to this language as 'turing complete' (a catchphrase that really means nothing since doing anything beyond simplistic manipulation would lend itself towards insanity).  This makes this language completely perfect for modding systems in games, or to allow designers to tweak values.  The last thing I want to note (well rather one of the last) is that the system is whitespace independent (basically opposite of python), this means it can be compacted down immensely for example the first example without whitespace (note I've removed comments since those require a 'newline' obviously);
```C
@Test=System.Color...;.RGB=255,64,128@TheSame=System.Color...;.RGB(Normalised)=1,0.25,0.5@AgainSame=System.Color...;.RGB(Hex)=0xFF3278;.Name="OtherName@Copy=System.Color...;.RGB=Test.RGB;.Name="Copy"
```
Compared to the shorter JSON;
```C
[{"Name":"Test","Red":255,"Green":64,"Blue":128},{"Normalised_Red":1,"Normalised_Green":0.25,"Normalised_Blue":0.5},{"Color_Hex":"FF4080","Name":"OtherName"},{"Name":"Copy","Copying":"Test"}]
```
As you can see the character count is around the same but in the actual readable variant its much shorter, this is due to how the syntax is structured which allows you to put things like RGB into a single line and so on.

## All nice and 'dandy' but how do I add my own stuff and objects?
