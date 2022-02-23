using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class responsible for all the out-of-unity executions, as well as preparing and storing execution data.
/// </summary>
public class ExecutionManager : MonoBehaviour {

    public NotepadManager NM;
    public TextManager TM;
    public Button readyButton;

    private int timeout;

    #region codeQuestionStrings
    private string label;
    private string[] hints;
    private string baseTests;
    private string finalTests;
    private string specificWrapper;
    private string codeQuestionSetup;
    private string intendedSolution;
    private string substitutionFunction;
    #endregion

    #region PubliccodeQuestionData
    public string functionType;
    public string functionName;
    public string[] argumentsType;
    public string[] argumentsName;
    public inputLimits[] argumentsLimits;
    #endregion

    #region Catch2Requirements
    public TextAsset main;
    public TextAsset imports;
    public TextAsset wrapper;
    #endregion

    void Start() {
        timeout = DataManager.currentTimeout;

        Setup();
        TM.PassHints(hints);
        TM.PassLabel(label);
        CleanTempDir();
    }


    #region StringGetters
    /// <summary>
    /// Getter for the base tests of the current codeQuestion.
    /// </summary>
    /// <returns>The contents of the [basic] tests.</returns>
    public string GetBaseTests() { return baseTests + "\n\n"; }

    /// <summary>
    /// Getter for the final tests of the current codeQuestion.
    /// </summary>
    /// <returns>The contents of the [final] tests.</returns>
    public string GetFinalTests() { return finalTests + "\n\n"; }


    /// <summary>
    /// Getter for the intended solution of the current codeQuestion.
    /// </summary>
    /// <returns>The contents of the solution function from the codeQuestion file.</returns>
    public string GetIntendedSolution() {
        string importsHeader = imports.ToString();

        string readyCpp = importsHeader + timeout + "\n\n";
        readyCpp += intendedSolution + "\n\n";
        readyCpp += specificWrapper + "\n\n";

        return readyCpp;
    }


    /// <summary>
    /// Utility function to obtain a solution after a possible main function substitution.
    /// </summary>
    /// <param name="suppliedSolution">String to use instead of the user notepad.</param>
    /// <returns>The solution, either given or from the notepad, after a possible main function substitution.</returns>
    public string GetUserSolution(string suppliedSolution) {

        //If no string is given, it will be taken from the user notepad
        if (string.IsNullOrEmpty(suppliedSolution)) {
            suppliedSolution = NM.GetSolution();
        }

        //If the codeQuestion does not require main function substitution, return the solution
        if (string.IsNullOrEmpty(substitutionFunction)) {
            return suppliedSolution;
        } else {
            return SubstituteFunction(suppliedSolution);
        }

    }
    #endregion

    /// <summary>
    /// Utility function to substitute the function signature with the <see cref="substitutionFunction"/>.
    /// </summary>
    /// <param name="inputString">Content of the user solution.</param>
    /// <returns>The <paramref name="inputString"/> but with the function signature changed into the <see cref="substitutionFunction"/>.</returns>
    private string SubstituteFunction(string inputString) {

        int beginning = inputString.IndexOf(functionType);

        if (beginning == -1) {
            UnityEngine.Debug.LogError("Error, user has changed the signature of the function to be tested and the execution cannot continue.");
            return null;
        }

        int nameIdx = 0;
        bool found = false;
        int finalIdx = -1;

        for (int i = beginning + functionType.Length + 1; i < inputString.Length; i++) {
            if (string.IsNullOrWhiteSpace("" + inputString[i])) { continue; }

            if (found) {
                if (inputString[i] == '{') {
                    finalIdx = i;
                    break;
                } else {
                    continue;
                }
            }


            if (inputString[i] == functionName[nameIdx]) {
                nameIdx += 1;
            } else {
                nameIdx = 0;
            }

            if (nameIdx == functionName.Length) {
                found = true;
            }

        }

        if (finalIdx == -1) {
            UnityEngine.Debug.LogError("Error, the solution does not contain the required function to be executed.");
            return null;
        }

        return inputString.Replace(inputString.Substring(beginning, finalIdx - beginning + 1), substitutionFunction);

    }

    /// <summary>
    /// Removes all .exe and .cpp files from the temporary files directory 
    /// </summary>
    private void CleanTempDir() {
        string[] allFiles = Directory.GetFiles(Application.persistentDataPath);

        for (int i = 0; i < allFiles.Length; i++) {
            //Gets the extension, provided it is 3 characters long
            string extension = allFiles[i].Substring(allFiles[i].Length - 4, 4);
            if (extension == ".cpp" || extension == ".exe" ) {
                File.Delete(allFiles[i]);
            }
        }
    }

    /// <summary>
    /// Utility function to setup a <see cref="codeQuestion"/> correctly.
    /// It is necessary to parse the <see cref="codeQuestion"/> .txt file to obtain the data required
    /// to write the wrapper correctly, since many parts depend on the starting function
    /// It calls the <see cref="Wrap(string)"/> function to create the specific wrapper.
    /// It calls the <see cref="ParseLimits(string)"/> function to create the <see cref="codeQuestion"/> limits. 
    /// Fails if the //_hints_// field is badly formatted.
    /// </summary>
    private void Setup() {

        string codeQuestionString = DataManager.currentCodeQuestion.content;

        string[] parts = codeQuestionString.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);

        string questionString = "";
        string limitString = "";
        string hintString = "";

        label = parts[0].Trim();

        //Between the _label_ and _main_
        codeQuestionSetup = parts[1].Trim() + "\n\n";
        intendedSolution = parts[1].Trim() + "\n\n";


        for (int i = 2; i < parts.Length; i++) {

            switch (parts[i - 1]) {
                case "_main_": {
                    //_main_ is part of the setup ONLY if _question_ is in the txt, otherwise it's just the question
                    if (Array.IndexOf(parts, "_question_") != -1) {
                        codeQuestionSetup += parts[i].Trim() + "\n\n";
                    }
                    intendedSolution += parts[i].Trim() + "\n\n";

                    questionString = parts[i].Trim();
                    if (NM != null && string.IsNullOrEmpty(NM.GetSolution())) { NM.SetSolution(parts[i].Trim().Split('{')[0] + "{};"); }
                    break;
                }

                case "_question_": {
                    (string[] subTypeAndName, string[] subArgs) = ExtractFunction(parts[i]);
                    substitutionFunction = subTypeAndName[0] + " " + subTypeAndName[1] + "(" + string.Join(",", subArgs) + "){";
                    intendedSolution += parts[i].Trim() + "\n\n";
                    break;
                }

                case "_limits_": {
                    limitString = parts[i].Trim();
                    break;
                }

                case "_hints_": {
                    hintString = parts[i].Trim();
                    break;
                }

                case "_base_": {
                    baseTests = parts[i].Trim();
                    break;
                }

                case "_final_": {
                    finalTests = parts[i].Trim();
                    break;
                }
            }
        }

        hints = ParseHints(hintString);

        specificWrapper = Wrap(questionString);

        //Now that all the information of the functions is saved, it is easier to parse the limits of the inputs
        argumentsLimits = ParseLimits(limitString);

    }

    /// <summary>
    /// Utility funtion to parse the ints.
    /// Does not allow 2 consecutive commas and trims every label.
    /// Trimming is important because the hint MUST start with an underscore (_).
    /// </summary>
    /// <param name="hintString">String of hints from the <see cref="codeQuestion"/> file.</param>
    /// <returns>The correctly formatted array of hint strings.</returns>
    private string[] ParseHints(string hintString) {
        string[] allHints = hintString.Split(new[] { "," }, StringSplitOptions.None);
        for (int i = 0; i < allHints.Length; i++) {
            allHints[i] = allHints[i].Trim();

            if (allHints[i].Length == 0) {
                UnityEngine.Debug.LogError("Error, the hints of this codeQuestion are badly formatted.");
                return null;
            }
        }
        return allHints;
    }


    /// <summary>
    /// Utility function to write the wrapper of the codeQuestion correctly
    /// Given the starting function, saves type, name and arguments, 
    /// then replaces them appropriately in the wrapper file.
    /// Fails if the starting function is badly formatted.
    /// </summary>
    /// <param name="info">Main function content of the codeQuestion.</param>
    /// <returns>The specific wrapper for the current codeQuestion.</returns>
    private string Wrap(string info) {
        string result = wrapper.ToString();

        (string[] typeAndName, string[] args) = ExtractFunction(info);

        string[] argsNames = new string[args.Length];
        string[] argsTypes = new string[args.Length];

        for (int i = 0; i < args.Length; i++) {
            string[] temp = args[i].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
            if (temp.Length != 2) {
                UnityEngine.Debug.LogError("Error, could not identify arguments of function: " + typeAndName[1]);
                return null;
            }
            argsTypes[i] = temp[0];
            argsNames[i] = temp[1];
        }

        string argsName = string.Join(",", argsNames);
        string argsAmperstand = "&" + string.Join(",&", argsNames);

        //Save the single information
        functionType = typeAndName[0];
        functionName = typeAndName[1];
        argumentsType = argsTypes;
        argumentsName = argsNames;

        //Replaces the information in the generic wrapper to make it specific
        result = result.Replace("//_type_//", typeAndName[0]);
        result = result.Replace("//_name_//", typeAndName[1]);
        result = result.Replace("//_full_arguments_//", string.Join(",", args));
        result = result.Replace("//_&_arguments_//", argsAmperstand);
        result = result.Replace("//_name_arguments_//", argsName);

        return result;
    }

    /// <summary>
    /// Utility function to extract a function signature of the format:
    /// type name(arguments)
    /// from a given list.
    /// </summary>
    /// <param name="input">String containing a function signature.</param>
    /// <returns>Tuple containing a string array with type and name of the function 
    /// and a string array with type and name of each argument.</returns>
    private (string[], string[]) ExtractFunction(string input) {
        string[] parts = input.Trim().Split(new[] { '(', ')' }, StringSplitOptions.None);

        if (parts.Length < 2) {
            UnityEngine.Debug.LogError("Error, could not identify the function signature.");
            return (null, null);
        }

        string[] typeAndName = parts[0].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);

        if (typeAndName.Length != 2) {
            UnityEngine.Debug.LogError("Error, could not identify the function signature.");
            return (null, null);
        }

        string[] args = parts[1].Split(new[] { ',' }, StringSplitOptions.None);
        return (typeAndName, args);
    }


    /// <summary>
    /// Utility function to parse the limits in the current <see cref="codeQuestion"/> file.
    /// </summary>
    /// <param name="limitString">String containing all specified limits.</param>
    /// <returns>An array of <see cref="inputLimits"/> ordered the same way as <see cref="argumentsName"/>.</returns>
    private inputLimits[] ParseLimits(string limitString) {
        if (string.IsNullOrEmpty(limitString)) { return null; }

        inputLimits[] limits = new inputLimits[argumentsName.Length];

        string[] linesOfLimits = limitString.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        //No limits, everything goes
        if(linesOfLimits.Length == 0) { return null; }

        //In case a variable is not named in the limits, there is no limit
        //However the default value of the tuple is (false, 0, false, 0) which would invalidate any number or string length
        //This is why we do this array iteration first
        foreach(inputLimits singleLimit in limits) { singleLimit.Init(); }

        foreach(string limitLine in linesOfLimits){

            if(limitLine.IndexOf("::") == -1) {
                UnityEngine.Debug.LogError("Error, limit format violated, ignoring all supplied input limits.");
                return null;
            }

            string[] parts = limitLine.Split(new[] { "::" }, StringSplitOptions.None);
            if(parts.Length != 2) {
                UnityEngine.Debug.LogError("Error, limit format violated, ignoring all supplied input limits.");
                return null;
            }


            parts[0] = parts[0].Trim();
            parts[1] = parts[1].Trim();

            inputLimits singleLimit = CreateLimit(parts[1]);

            if (singleLimit.isMalformed) {
                UnityEngine.Debug.LogError("Error, limit format violated, ignoring all supplied input limits.");
                return null;
            }

            string[] namesInLimit = parts[0].Split(new[] { ',' }, StringSplitOptions.None);
            string[] valuesInLimit = DataManager.CommaSplitPreservingQuotes(parts[1].Substring(1, parts[1].Length - 2), -1);


            for (int j = 0; j < namesInLimit.Length; j++) {
                namesInLimit[j] = namesInLimit[j].Trim();

                //Find the index of the argument
                int idx = Array.IndexOf(argumentsName, namesInLimit[j]);
                if(idx == -1) {
                    UnityEngine.Debug.LogError("Error, limit format violated, ignoring all supplied input limits.");
                    return null;
                }

                if(singleLimit.setValues != null) {
                    //Checking that the value belongs to a set of the correct type
                    for (int k = 0; k < valuesInLimit.Length; k++) {
                        valuesInLimit[k] = valuesInLimit[k].Trim();

                        if(!DataManager.IsCorrectType(valuesInLimit[k], argumentsType[idx])) {
                            UnityEngine.Debug.LogError("Error, limit format violated, ignoring all supplied input limits.");
                            return null;
                        }
                        singleLimit.setValues.Add(valuesInLimit[k]);
                    }
                }

                limits[idx] = singleLimit;
            }
        }
        return limits;
    }

    /// <summary>
    /// Utility function to parse the rightside of a limit format and create the corresponding <see cref="inputLimits"/>.
    /// </summary>
    /// <param name="limitFormat">Rigthside of a limit format, like (1, 3], { 'd', 'c' } or [0, :).</param>
    /// <returns>The <see cref="inputLimits"/> corresponding to the given <paramref name="limitFormat"/>,
    /// the property <see cref="inputLimits.isMalformed"/> is set to true if the input was malformed.</returns>
    private inputLimits CreateLimit(string limitFormat) {
        inputLimits singleLimit = new inputLimits();
        singleLimit.isMalformed = true;

        switch (limitFormat[0]) {
            case '(': singleLimit.leftIncluded = false; break;
            case '[': singleLimit.leftIncluded = true; break;
            case '{': singleLimit.setValues = new List<string>(); break;
            default:
                return singleLimit;
        }

        switch (limitFormat[limitFormat.Length - 1]) {
            case ')':
                if (singleLimit.setValues == null) {
                    singleLimit.rightIncluded = false;
                } else {
                    return singleLimit;
                }
                break;
            case ']':
                if (singleLimit.setValues == null) {
                    singleLimit.rightIncluded = true;
                } else {
                    return singleLimit;
                }
                break;
            case '}':
                if (singleLimit.setValues == null) {
                    return singleLimit;
                }
                break;
            default:
                return singleLimit;
        }


        string[] valuesInLimit = DataManager.CommaSplitPreservingQuotes(limitFormat.Substring(1, limitFormat.Length - 2), -1);
        if (valuesInLimit == null) { return singleLimit; }

        if (singleLimit.setValues == null) {
            if (valuesInLimit.Length != 2) { return singleLimit; }

            if (!int.TryParse(valuesInLimit[0], out singleLimit.leftValue)) {
                if (valuesInLimit[0].Trim() != ":") {
                    return singleLimit;
                } else {
                    singleLimit.leftValue = int.MinValue;
                }
            }

            if (!int.TryParse(valuesInLimit[1], out singleLimit.rightValue)) {
                if (valuesInLimit[1].Trim() != ":") {
                    return singleLimit;
                } else {
                    singleLimit.rightValue = int.MaxValue;
                }
            }


        } else {
            if (valuesInLimit.Length < 2) { return singleLimit; }
        }

        singleLimit.isMalformed = false;
        return singleLimit;
    }

    #region ExternalProcessExecutions

    /// <summary>
    /// Utility function to setup a <see cref="Process"/>, start it, wait for it to finish and dispose of it.
    /// The function is 'async' because the execution of the command could take arbitrarely long.
    /// The function returns a <see cref="Task"/> because we need the result of the execution.
    /// </summary>
    /// <param name="executionProgram">Name of the program to start.</param>
    /// <param name="command">Command that will be given to the <paramref name="executionProgram"/> as argument.</param>
    /// <returns>The result of the execution as it would have been printed to standard output.</returns>
    private async Task<string> ExecuteProcess(string executionProgram, string command) {
        Process exeProcess = new Process();
        exeProcess.StartInfo.WorkingDirectory = Application.persistentDataPath + "/";
        exeProcess.StartInfo.CreateNoWindow = true;
        exeProcess.StartInfo.UseShellExecute = false;
        exeProcess.StartInfo.RedirectStandardOutput = true;

        exeProcess.StartInfo.FileName = executionProgram;
        exeProcess.StartInfo.Arguments = command;

        exeProcess.Start();

        string commandResult = await exeProcess.StandardOutput.ReadToEndAsync();

        exeProcess.WaitForExit();
        exeProcess.Dispose();

        return commandResult;
    }

    /// <summary>
    /// Function to create the file catch_main.o.
    /// The file it's required for execution but takes some time to compile ,
    /// so it is compiled once at the beginning and then linked during the next compilations.
    /// The function is 'async' because the compilation takes some time (usually between 10 and 45 seconds).
    /// The function returns a <see cref="Task"/> becuase we need the result of the compilation.
    /// </summary>
    /// <returns>The result of the compilation, if it is not empty something went wrong.</returns>
    public async Task<string> PreCompile() {
        //Transform the textAsset in a .cpp first
        File.WriteAllText(Application.persistentDataPath + "/catch_main.cpp", main.ToString());

        string executionProgram;
        string command;

#if UNITY_STANDALONE_LINUX
        executionProgram = "bash";
        command = "g++ -O3 -g0 -Werror -Wall -fuse-ld=lld catch_main.cpp -c 2>&1; exit";
#else
        executionProgram = "cmd.exe";
        command = "/C g++ -O3 -g0 -Werror -Wall -fuse-ld=lld catch_main.cpp -c 2>&1";
#endif

        return await ExecuteProcess(executionProgram, command);
    }

    /// <summary>
    /// Function to compile the user solution.
    /// The function is 'async' because the compilation takes some time (usually between 10 and 30 seconds).
    /// The function returns a <see cref="Task"/> because we need the result of the compilation.
    /// </summary>
    /// <param name="fileName">The name of the file to compile, with extension excluded.</param>
    /// <returns>The result of the compilation, if it is not empty something went wrong.</returns>
    public async Task<string> Compile(string fileName) {
        string executionProgram;
        string command;

#if UNITY_STANDALONE_LINUX
        executionProgram = "bash";
        command = "g++ -O3 -g0 -Werror -Wall -fuse-ld=lld catch_main.o " + fileName + ".cpp -o " + fileName + " 2>&1; exit";

#else
        executionProgram = "cmd.exe";
        command = "/C g++ -O3 -g0 -Werror -Wall -fuse-ld=lld catch_main.o " + fileName + ".cpp -o " + fileName + " 2>&1";
#endif

        return await ExecuteProcess(executionProgram, command);
    }

    /// <summary>
    /// Overload to compile the user solution using the <see cref="codeQuestion"/> name as filename.
    /// The function is 'async' because the compilation takes some time (usually between 10 and 30 seconds).
    /// The function returns a <see cref="Task"/> because we need the result of the compilation.
    /// </summary>
    /// <returns>The result of the compilation, if it is not empty something went wrong.</returns>
    private async Task<string> Compile() { return await Compile(DataManager.currentCodeQuestion.name); }

    /// <summary>
    /// Function to execute the compiled executable.
    /// Depending on the round, the tests that are executed change using the Catch2 [tags].
    /// The function is 'async' because the execution takes some time (usually less than 2 seconds).
    /// The function returns a <see cref="Task"/> because we need the result of the compilation.
    /// </summary>
    /// <param name="fileName">Name of the executable to test.</param>
    /// <param name="tags">Tags to select which tests to execute, [base], [user], [final] or no tag, meaning all of them.</param>
    /// <returns>The result of the execution, if it is empty something went wrong.</returns>
    public async Task<string> Test(string fileName, string tags) {
        string executionProgram;
        string command;

#if UNITY_STANDALONE_LINUX
		executionProgram = "bash";
		command = fileName + " -s -r compact " + tags + "; exit";
#else
        executionProgram = "cmd.exe";
        command = "/C " + fileName + " -s -r compact " + tags;
#endif

        return await ExecuteProcess(executionProgram, command);
    }

    /// <summary>
    /// Overload to execute the compiled executable, with the same name as the <see cref="codeQuestion"/> name,
    /// with no tags, meaning that all tests will be run.
    /// The function is 'async' because the execution takes some time (usually less than 2 seconds).
    /// The function returns a <see cref="Task"/> because we need the result of the compilation.
    /// </summary>
    /// <returns>The result of the execution, if it is empty something went wrong.</returns>
    public async Task<string> Test() { return await Test(DataManager.currentCodeQuestion.name, ""); }

    /// <summary>
    /// Function to start the basic tests on the user solution, 
    /// compilation errors and result of the execution are printed to the user log.
    /// The function is public void and parameterless so that it can be called by a button OnClick.
    /// The function is 'async' because the compilation and execution takes time (between 10 and 30 seconds usually).
    /// The function is 'async void' because it adheres to the "fire and forget" pattern, its termination is signaled by a side effect (the button becoming active)
    /// </summary>
    public async void BasicTest() {
        Boilerplate();

        string compilerResult = await Compile();

        //Compilation failed
        if (compilerResult.Trim().Length > 0) {
            TM.AddToLog(compilerResult.Trim());
            return;
        }

        string executionResult = await Test();

        (bool noFailures, string parsedResult) = ParseToLog(executionResult.Trim());

        //When all tests pass, the ready button should be enabled
        if (noFailures) { readyButton.interactable = true; }

        TM.AddToLog(parsedResult);
    }

    /// <summary>
    /// Function to start the creation, compilation and execution of all tests of the given solution.
    /// The function is 'async' because the execution takes some time (usually around 20 seconds).
    /// The function returns a <see cref="Task"/> because we need the result of the execution.
    /// </summary>
    /// <param name="solution">A solution already filled with boilerplate and doubts.</param>
    /// <param name="filename">The name to use for the .cpp and .exe names.</param>
    /// <returns></returns>
    public async Task<string> TestReadySolution(string solution, string filename) {
        Create(solution, filename);

        string result = await Compile(filename);
        if (!string.IsNullOrEmpty(result)) {
            UnityEngine.Debug.LogError("Error, compilation of the server solution failed, the codeQuestion file probably contains an error.");
            return null;
        }

        result = await Test(filename, "");

        return result;
    }

    #endregion

    #region FileCreation

    /// <summary>
    /// Function that creates a .cpp file containing exactly what is written on a user solution.
    /// The user can ask to have a copy of its answer or of the best answer in the path that it selected in the settings.
    /// The function is public void and single parameter so that it can be called by a button OnClick.
    /// </summary>
    /// <param name="notMine">true if the .cpp should be created from the best solution in the lobby, 
    /// false if the .cpp should be created from the solution of the current user.</param>
    public void Create(bool notMine) {
        string readyCpp = DataManager.GetClientSolution(DataManager.myData.owner);
        string owner = "my_";

        if (notMine) {
            readyCpp = DataManager.solutions[0][0];
            owner = "best_";
        }

        File.WriteAllText(DataManager.currentPath + "/" + owner + DataManager.currentCodeQuestion.name + ".cpp", readyCpp);
    }

    /// <summary>
    /// Overload to create a .cpp file containing what is given in input in the temporary directory.
    /// </summary>
    /// <param name="solution">Content to be inserted in the .cpp.</param>
    /// <param name="fileName">New name of the .cpp that will be created.</param>
    public void Create(string solution, string fileName) {
        File.WriteAllText(Application.persistentDataPath + "/" + fileName + ".cpp", solution);
    }

    /// <summary>
    /// Overload to create a .cpp containing what is given in input in the temporary directory, 
    /// using the name of the <see cref="codeQuestion"/> as filename.
    /// The function is public void and single parameter so that it can be called by a button OnClick.
    /// </summary>
    /// <param name="solution">Content to be inserted in the .cpp.</param>
    public void Create(string solution) { Create(solution, DataManager.currentCodeQuestion.name); }

    /// <summary>
    /// Funtion to create a .cpp file for all known user solutions (server only).
    /// The function is public void and parameterless so that it can be called by a button OnClick.
    /// </summary>
    public void CreateAll() {
        for(int i = 0; i < DataManager.lobbiesInUse; i++) {
            for(int j = 0; j < DataManager.solutions[i].Length; j++) {

                //Insert the username at the beginning of the file in a comment for easier recognition
                string readyCpp = "//" + DataManager.leaderboard[i][j].username + "\n\n"; 

                //Add the boilerplate of the user solutioon
                readyCpp += Boilerplate(DataManager.solutions[i][j], false);

                //Add the standard tests
                readyCpp += GetBaseTests() + GetFinalTests();

                //Finally create a .cpp for the server
                Create(readyCpp, "Client" + DataManager.leaderboard[i][j].owner);
            }
        }
    }

    #endregion

    /// <summary>
    /// Utility function to create a valid solution.
    /// Every solution requires:
    /// The import header and a definition of the TIMEOUT value.
    /// The additional setup code required to run the codeQuestion, if it exists.
    /// The main solution.
    /// The correct codeQuestion wrapper and the base tests.
    /// Call this function before every compilation.
    /// </summary>
    /// <param name="solution">Main solution to include.</param>
    /// <param name="createFile">true if a cpp file should be created, false otherwise.</param>
    /// <returns>The string containing a solution in the valid format.</returns>
    public string Boilerplate(string solution, bool createFile) {
        string importsHeader = imports.ToString();

        string readyCpp = importsHeader + timeout + "\n\n";
        readyCpp += codeQuestionSetup + "\n\n";
        readyCpp += solution + "\n\n";
        readyCpp += specificWrapper + "\n\n";

        if (createFile) {
            readyCpp += GetBaseTests();
            Create(readyCpp); 
        }

        return readyCpp;
    }

    /// <summary>
    /// Overload of <see cref="Boilerplate(string, bool)"/>, it creates a valid solution from the user notepad and
    /// creates its corresponding cpp file.
    /// </summary>
    /// <returns>The string containing the solution in the valid format.</returns>
    public string Boilerplate() { return Boilerplate(GetUserSolution(null), true); }

    /// <summary>
    /// Utility function to parse the result of the compilation and execution of the user solution.
    /// </summary>
    /// <param name="testResults">Output of the basic execution or, if it failed, the error message of the compilation.</param>
    /// <returns>A tuple containing a bool that is true if no tests have failed, false otherwise and 
    /// an edited string of the execution result to be added to the user log.</returns>
    private (bool, string) ParseToLog(string testResults) {

        //Split the output in rows, some are stdout and some are actual test results
        string[] resultLines = testResults.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        //Required to know if the solution can be considered ready for submittion
        bool passedAll = true;

        //Each array element needs to be treated differently:
        //stdout should be printed as found
        //passed tests should say: passed: functionName(args) returned X as expected
        //failed normal tests should say: failed: functionName(args) should have returned: X instead of Y
        //failed exception tests should say: failed: functionName(args) should have returned: X instead of raising exception Y
        //failed exception crash should say: failed: we lost a test that fatally crashed: crashMessage
        for (int i = 0; i < resultLines.Length; i++) {
            int questionIdx = resultLines[i].IndexOf(DataManager.currentCodeQuestion.name + ".cpp:");

            //Actual test result
            if (questionIdx >= 0) {
                //In case the stdout doesn't have an endl separating it from the tests
                string baseString = "";
                if (questionIdx > 0) { baseString = resultLines[i].Substring(0, questionIdx) + "\n"; }

                //foundBad represents what the function returned
                string foundBad;
                int start = resultLines[i].IndexOf("with message: '");
                if (start >= 0) {
                    //The "bad" is either the message of an exception or a fatal crash
                    foundBad = DataManager.ExtractFromStringToChar(resultLines[i], "with message: '", '\'');
                } else {
                    //The "bad" is a bad return value
                    foundBad = DataManager.ExtractFromStringToChar(resultLines[i], " for: ", '=');
                }

                //Having found a single failed test case means that the solution is not ready to be submitted
                if (resultLines[i].IndexOf("failed: ") >= 0) {
                    passedAll = false;

                    if (resultLines[i].IndexOf("fatal error condition") >= 0) {
                        resultLines[i] = baseString + "failed: we lost a test that fatally crashed: " + foundBad;
                        continue;
                    }

                    //foundArgs represents which arguments were given for this test, excluding the leading TIMEOUT parameter
                    string foundArgs = DataManager.ExtractFromStringToChar(resultLines[i], ",", ')');


                    //foundRes represents what the function should have returned
                    string foundRes = DataManager.ExtractFromStringToChar(resultLines[i], "== ", ' ');


                    if (resultLines[i].IndexOf("unexpected exception") >= 0) {
                        resultLines[i] = baseString + "failed: " + functionName + "(" + foundArgs + ") should have returned: " + foundRes + " instead of raising exception: " + foundBad;
                        continue;
                    }


                    resultLines[i] = baseString + "failed: " + functionName + "(" + foundArgs + ") should have returned: " + foundRes + " instead of: " + foundBad;

                } else {
                    //foundArgs represents which arguments were given for this test, excluding the leading TIMEOUT parameter
                    string foundArgs = DataManager.ExtractFromStringToChar(resultLines[i], ",", ')');

                    resultLines[i] = baseString + "passed: " + functionName + "(" + foundArgs + ") returned: " + foundBad + " as expected";
                }
            }

        }
        return (passedAll, string.Join("\n", resultLines));
    }


}
