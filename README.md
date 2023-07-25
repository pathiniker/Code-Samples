# C# Code Samples

I selected these scripts to highlight an advanced Unity Editor tool (CopyCurriculumUtility), and some modular UI tech.
### Note:
Each folder uses a different syntax style, according to the workplace they were built for.
The scripts in the Websocket Chat UI demonstrate my personal preferred syntax style.

## Smash Routes Curriculum Tools
### CopyCurriculumUtility
#### Overview:
This editor utility script is designed to solve an issue presented when manually copying a complex object or directory in the Unity Editor.

#### Context:
A 'curriculum' is a master scriptable object that contains a massive amount of settings and variables, and data in the form of scriptable objects that are serialized on the curriculum object.
Each curriculum lives in its own parent directory, where all serialized scriptable objects live in subfolders of the parent directory.

#### The problem:
The editor user wants to be able to copy an existing curriculum in order to use it as a base for a new one.
The user has been doing this by simply duplicating the folder structure, renaming the curriculum object, and proceeding to modify its properties from there.
However, the user has realized that all of the serialized references to the nested scriptable objects on the curriculum point not to the scriptable objects in this new directory, but instead to the scriptable objects in the old directory.

#### The solution:
I needed to create a tool that would automate this duplication process, and in that process, ensure that the new curriculum referenced the new copies of all children scriptable objects.
To accomplish this, one of the key functions of this utility is reading the raw YAML file of the new curriculum object itself, and replacing the GUIDs of all of the serialized children objects with the corresponding GUID of the new copied objects.
Then, in order for Unity to properly recognize the new GUID associations on the YAML file, Unity needs to restart - a function that is incorporated into the automated process.

## Websocket Chat UI
#### Overview:
These are a few of the scripts that are used in an in-game chat window, modeled to function the same way as Overwatch's chat window - containing multiple chat feeds: global chat, direct user chat, etc.

### AModal
This is the base class I use for any form of pop-up window in my projects.
It contains a fully overridable hide and display sequence, that is triggered through a single DisplayModal(bool) function.
I included the UI_TransitionableComponent script since it is referenced by this script.