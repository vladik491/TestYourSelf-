using UnityEngine;
using System.Collections.Generic;

public class Preloader : MonoBehaviour
{
    public static Dictionary<string, Texture2D[]> gifFramesCache = new Dictionary<string, Texture2D[]>();

    void Awake()
    {
        // Preload всех папок GIF
        PreloadGif("AttackOnTitan");
        PreloadGif("Bleach");
        PreloadGif("BreakingBad");
        PreloadGif("DoctorWho");
        PreloadGif("GameOfThrones");
        PreloadGif("HarryPotter");
        PreloadGif("Marvel");
        PreloadGif("MyHeroAcademia");
        PreloadGif("Naruto");
        PreloadGif("OnePiece");
        PreloadGif("PiratesOfTheCaribbean");
        PreloadGif("Pokemon");
        PreloadGif("Sherlock");
        PreloadGif("StarWars");
        PreloadGif("StrangerThings");
        PreloadGif("Supernatural");
        PreloadGif("TheLordOfTheRings");
        PreloadGif("Transformers");
    }

    private void PreloadGif(string folderName)
    {
        Texture2D[] frames = Resources.LoadAll<Texture2D>($"GifFrames/{folderName}");
        if (frames.Length > 0)
        {
            gifFramesCache[folderName] = frames;
        }
    }
}