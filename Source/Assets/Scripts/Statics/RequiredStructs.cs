using Unity.Collections;
using System.Collections.Generic;

/// <summary>
/// Struct representing the data associated with a user.
/// Because of username and avatar being <see cref="FixedString32Bytes"/>, the whole struct CAN be sent over the network in a Rpc.
/// This struct's size is fixed at: 32 + 2 + 2 + 32 + 8 = 76 bytes.
/// </summary>
[System.Serializable]
public struct databaseEntry {
    public FixedString32Bytes username;
    public ushort progress;
    public ushort points;
    public FixedString32Bytes avatar;
    public ulong owner;

    //Complete constructor
    public databaseEntry(FixedString32Bytes u, ushort p, ushort s, FixedString32Bytes a, ulong o) {
        this.username = u;
        this.progress = p;
        this.points = s;
        this.avatar = a;
        this.owner = o;
    }

    //Constructor used for setting a default value for points different from 0
    public databaseEntry(ushort p) : this() {
        this.points = p;
    }

    //Deep copy constructor
    public databaseEntry(databaseEntry d) : this(d.username, d.progress, d.points, d.avatar, d.owner) { }

}

/// <summary>
/// Struct representing the data associated with a codeQuestion.
/// Because of its arbitrarely large nature, the properties are string so they CANNOT be sent over the network in a Rpc (packed in a struct).
/// This struct's size is unknown at compile time.
/// </summary>
[System.Serializable]
public struct codeQuestion {
    public string name;
    public string description;
    public string content;
    public string[] tags;

    //Complete constructor
    public codeQuestion(string n, string d, string c, string[] t) {
        this.name = n;
        this.description = d;
        this.content = c;
        this.tags = t;
    }
}

/// <summary>
/// Struct representing the data associated with a doubt.
/// because of the use of <see cref="FixedString32Bytes"/> and <see cref="FixedString128Bytes"/>, the whole struct CAN be sent over the network in a Rpc.
/// This struct's size is fixed at: 1 + 8 + 8 + 32 + 32 + 32 + 1 + 128 + 128 = 370 bytes. 
/// </summary>
[System.Serializable]
public struct doubt {
    public STATUS currentStatus;
    public ulong clientId;
    public ulong targetId;
    public FixedString32Bytes input;
    public FixedString32Bytes output;
    public FixedString32Bytes expected;
    public DOUBTTYPE doubtType;
    public FixedString128Bytes clientDoubt;
    public FixedString128Bytes serverDoubt;

    //Almost complete constructor (currentStatus is set at a default value)
    public doubt(ulong cId, ulong tId, string i, string o, string e, DOUBTTYPE t, string cd, string sd) {
        this.currentStatus = STATUS.None;
        this.clientId = cId;
        this.targetId = tId;
        this.input = new FixedString32Bytes(i);
        this.output = new FixedString32Bytes(o);
        this.expected = new FixedString32Bytes(e);
        this.doubtType = t;
        this.clientDoubt = new FixedString128Bytes(cd);
        this.serverDoubt = new FixedString128Bytes(sd);
    }

    /// <summary>
    /// Method to progress along the enum branches of the <see cref="doubt.currentStatus"/>.
    /// </summary>
    /// <param name="positiveBranch">true if the <see cref="STATUS"/> should progress on the positive branch, false otherwise.</param>
    public void ProgressStatus(bool positiveBranch) {
        STATUS status = this.currentStatus;

        if (positiveBranch) {
            switch (status) {
                case STATUS.None: {
                    status = STATUS.ServerOk;
                    break;
                }
                case STATUS.ServerOk: {
                    status = STATUS.BothOk;
                    break;
                }
                case STATUS.ServerNo: {
                    status = STATUS.UserOk;
                    break;
                }
                case STATUS.UserOk: {
                    status = STATUS.Lost;
                    break;
                }
                case STATUS.UserNo: {
                    status = STATUS.Angry;
                    break;
                }
                case STATUS.BothNo: {
                    status = STATUS.Regret;
                    break;
                }
            }

        } else {
            switch (status) {
                case STATUS.None: {
                    status = STATUS.ServerNo;
                    break;
                }
                case STATUS.ServerOk: {
                    status = STATUS.UserNo;
                    break;
                }
                case STATUS.ServerNo: {
                    status = STATUS.BothNo;
                    break;
                }
                case STATUS.BothOk: {
                    status = STATUS.Correct;
                    break;
                }
                case STATUS.UserOk: {
                    status = STATUS.Lucky;
                    break;
                }
                case STATUS.UserNo: {
                    status = STATUS.Scared;
                    break;
                }
                case STATUS.BothNo: {
                    status = STATUS.Worst;
                    break;
                }
            }
        }

        this.currentStatus = status;
    }


}

/// <summary>
/// Enum representing the type of a <see cref="doubt"/>.
/// The type depends on if the user pressed on the special buttons (no compilation, timeout and crash) instead of inserting an expected value.
/// </summary>
[System.Serializable]
public enum DOUBTTYPE : byte {
    Regular,
    NoCompilation,
    Timeout,
    Crash
}


/// <summary>
/// Enum representing the state of a <see cref="doubt"/>.
/// The state depends on 3 binary values, creating a final amount of states of 2^3 and a total amount of states of 2^4 -1,
/// although because one result is impossible the final number of states is 7 and the total amount of states is 14.
/// </summary>
[System.Serializable]
public enum STATUS : byte {
    None,       //Doubt has not been judged yet

    ServerOk,   //Doubt predicts correctly the server solution, no info otherwise
    ServerNo,   //Doubt predicts wrongly   the server solution, no info otherwise

    BothOk,     //Doubt predicts correctly user output AND correctly server solution, final info unknown
    UserOk,     //Doubt predicts correctly user output BUT wrongly   server solution, final info unknown
    UserNo,     //Doubt predicts wrongly   user output BUT correctly server solution, final info unknown
    BothNo,     //Doubt predicts wrongly   user output AND wrongly   server solution, final info unknown

    Correct,    //Doubt predicts correctly user output AND correctly server solution, while the user output is DIFFERENT form the server solution
    Lost,       //Doubt predicts correctly user output BUT wrongly   server solution, while the user output is EQUAL       to the server solution
    Lucky,      //Doubt predicts correctly user output BUT wrongly   server solution, while the user output is DIFFERENT from the server solution
    Worst,      //Doubt predicts wrongly   user output AND wrongly   server solution, while the user output is DIFFERENT from the server solution
    Angry,      //Doubt predicts wrongly   user output BUT correctly server solution, while the user output is EQUAL       to the server solution
    Scared,     //Doubt predicts wrongly   user output BUT correctly server solution, while the user output is DIFFERENT from the server solution
    Regret,		//Doubt predicts wrongly   user output AND wrongly   server solution, while the user output is EQUAL       to the server solution
}


/// <summary>
/// Struct representing a limit imposed on a <see cref="codeQuestion"/> input argument.
/// Only ever used locally.
/// </summary>
[System.Serializable]
public struct inputLimits {
    public bool isMalformed;

    public int  leftValue;
    public int  rightValue;
    public bool leftIncluded;
    public bool rightIncluded;

    public List<string> setValues;

    public void Init() {
        this.leftValue = int.MinValue;
        this.rightValue= int.MaxValue;
    }
}

/// <summary>
/// Summary struct representing an initial parse of a test result line.
/// Only ever used locally.
/// </summary>
[System.Serializable]
public struct testLineContents {
    public string input;
    public string correct;
    public string expected;

    public int endOfInputIdx;
    public char followingChar;
}
