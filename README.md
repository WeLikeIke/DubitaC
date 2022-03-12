# DubitaC
  A serious game to support undergraduate programming courses.
  
  DubitaC (pronounced Doo-bee-ta-cee) was developed for the Master Thesis in Computer Science of Lorenzo Tibaldi at University of Genoa.
  
  The goal of the project was to create a product that would take advantage of gamification techniques to help students that are being introduced to the programming basics.
  
  The result was a serious game that is focused on:
  1. Creating solutions to programming problems in C++.
  2. Analysing and understanding the code of their peers.
  3. Reasoning about edge cases or unexpected behaviours.

## The game loop ##
DubitaC's main game loop asks users to solve a codeQuestion that was decided by the instructor/admin, then the users have a limited time to create a valid C++ solution to the problem.

After the solutions have been created, each user will be able to analyse the solution of the other users that were in its same lobby and, at the same time, are instructed of creating *doubts* about them.

At the end of the game session the final leaderboard depends on the performance of user solutions and user doubts against given test batteries.


## What is a doubt? ##
A doubt is composed of 4 parts:
1. A target solution and, implicitly, a doubter client.
2. A type:
	2.1. Doubting on non compilation.
	2.2. Doubting on a wrong return value.
	2.3. Doubting on a timeout.
	2.4. Doubting on an unexpected termination, either with an exception or worse.
3. An input that the doubter believes would cause trouble when given to the target solution, if the type is not "Non compilation".
4. An output that the doubter believes *should* be returned by a "perfect" solution when the input at point 2 is given, if the type is not "Non compilation".
5. An output that the doubter believes *will* be returned by the target solution when the input at point 2 is given, only if the type is "Returns a worng value".


The goal of the doubts is to create them correctly and reduce the score of the target, this is why it is important to understand the code and create the appropriate "output" doubt, since recognizing a no compilation, no termination or crash will grant the user a lot of points, while removing from the target even more.

## Technical details ##
DubitaC was developed using Unity and the experimental package [Netcode for GameObjects](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects).

The game takes advantage of the [Catch2 test framework for C++ programs](https://github.com/catchorg/Catch2), after the doubt are created, the game parses them and appends them to the user solutions as test cases.

After the compilation, if successfull, the resulting executables are run in a separate process.

At the end of the execution, the result is hijacked from the terminal to a variable, where it will be parsed to assign the points correctly.

DubitaC, in the current version, can **only** be played on a **Local network** on **Windows machines**.

The maximum number of concurrent players is **24**, divided in 4 lobbies, while the minimum is **2**, additionally, there must always be **one server online**.

The supported resolution are:
1. **16:9**
2. **16:10**
3. **4:3**
4. **1:1**
With different resolutions you might experience **UI clipping**.

## How to play ##
Automatic Install:
1. Download the latest installer on this repository.
2. Run it.
3. Follow the installation, making sure to select Server or Client build depending on your needs.
4. Inside the installation folder, select the Server or Client folder, run the executable DubitaC.exe


Manual Install
1. Download the ManualBuilds folder from this repository.
2. Extract it.
3. **Make sure to add to your environment variables the path: $InstallationDir$/UpToDateMinGw/bin**.
4. Both the Server and Client builds are included, select the chosen one.
5. Inside the chosen folder, run the executable DubitaC.exe.

In step 3, $InstallationDir$ should correspond to the directory where you extracted ManualBuilds.

**Step 3 cannot be skipped for manual install, it is not necessary with the installer, since it automatically runs a .bat file to add the path to the environment variable.**

## What can be found in this repository? ##
Since DubitaC is completely Open Source from version 1.3, the repository contains:
1. The complete source code, from scripts to Unity assets.
2. The 2 latest builds, for Server and Client.
3. The Automatic documentation created by doxygen.
4. The Installer and installation script for the latest version.
5. The original Thesis paper, with relative reference manual as an appendix.

Feel free to contribute!