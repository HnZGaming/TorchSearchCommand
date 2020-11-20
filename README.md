TorchSearchCommand
===

Performs approximate string matching, using Levenshtein distance algorithm. 

Currently provides a set of "search commands" to look up Torch commands and game entities in the server. 

Useful for: 

1. Searching for a command's name and syntax help.
1. Obtaining a complex grid/player name to the clipboard.

Commands are available to general players except game-breaking options.

How To Install
---

Prop it in the Plugin folder.

How To Use
---

### Search Torch Commands

```
!sc <keyword(s)> --limit=N
```

### Search Online Players

```
!sp <keyword(s)> -limit=N -copy -gps
```

### Search Grids

```
!sg <keyword(s)> -limit=N -copy -gps
```

### Options

#### -limit=N

Shows N number of search results.

#### -copy

Copies the top result to the clipboard.

#### -gps

Creates and shows the top result's GPS entity on the Player HUD.
