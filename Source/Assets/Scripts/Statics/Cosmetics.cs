using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Random = UnityEngine.Random;

/// <summary>
/// Static class to maintain data between the scenes, only deals with avatar <see cref="Sprite"/>s, their names and frequent <see cref="Color"/>s.
/// </summary>
public static class Cosmetics {

    #region ColorConstants
    public static Color buttonsColor =          new Color(1.000f, 0.898f, 0.000f);
    public static Color correctColor =          new Color(0.000f, 1.000f, 0.000f);
    public static Color wrongColor =            new Color(1.000f, 0.000f, 0.000f);
    public static Color selectableColor =       new Color(0.855f, 0.490f, 0.953f);
    public static Color selectedColor =         new Color(0.000f, 1.000f, 0.835f);

    #endregion

    private static SpriteAtlas avatars;
    private static TextAsset allAvatarNames;
    private static List<string> baseAvatarNames;

    /// <summary>
    /// Initialize the 3 fields of the <see cref="Cosmetics"/> static class, fails graciously returning null if the Resources are not found.
    /// </summary>
    public static void Init() {
        if (avatars == null) {
            try {
                avatars = Resources.Load("AvatarAtlas") as SpriteAtlas;
            } catch (Exception) {
                Debug.LogError("Error, failed loading the Cosmetics SpriteAtlas");
                avatars = null;
            }

        }

        if (allAvatarNames == null) {

            try {
                allAvatarNames = Resources.Load("AvatarNames") as TextAsset;
            } catch (Exception) {
                Debug.LogError("Error, failed loading the Cosmetics TextAsset");
                allAvatarNames = null;
            }

        }


        string[] names = GetAvatarNames();
        baseAvatarNames = new List<string>();

        //A base avatar is an avatar with a 0 points threshold
        for (int i = 0; i < names.Length; i++) {
            if (names[i].Substring(names[i].Length - 2, 2) == "_0") {
                baseAvatarNames.Add(names[i]);
            }
        }
    }

    /// <summary>
    /// Function to check if the initialization terminated.
    /// </summary>
    /// <returns>true if successful, false if the initialization failed.</returns>
    public static bool IsReady() { return (avatars != null && allAvatarNames != null); }


    #region CosmeticsGetters
    /// <summary>
    /// Retrieves a Sprite from the <see cref="SpriteAtlas"/> by name and state.
    /// </summary>
    /// <param name="name">The name of the sprite, it MUST contain the price at the end (Cat_0, for example)</param>
    /// <param name="state">A number between 0 and 3: 0 = deafult, 1 = unknown, 2 = losing, 3 = winning</param>
    /// <returns>Returns the single <see cref="Sprite"/ according to the inputs, null if the inputs are wrong or <see cref="Cosmetics"/> was not initialized.</returns>
    public static Sprite GetAvatar(string name, int state) {
        if (name == null || !IsReady()) { return null; }
        return avatars.GetSprite(name + "_" + state);
    }

    /// <summary>
    /// Function to retieve the names of all the Ssrites in <see cref="allAvatarNames"/>.
    /// </summary>
    /// <returns>Returns a string array containing all sprite name (price included, state excluded), null if <see cref="Cosmetics"/> was not initialized.</returns>
    public static string[] GetAvatarNames() {
        if (!IsReady()) { return null; }
        return allAvatarNames.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Function to return a random name between all sprites that have a price of 0.
    /// </summary>
    /// <returns>A single string representing the name of a random Sprite with price of 0.</returns>
    public static string GetRandomAvatarName() {
        return baseAvatarNames[Random.Range(0, baseAvatarNames.Count)];
    }

    #endregion
}
