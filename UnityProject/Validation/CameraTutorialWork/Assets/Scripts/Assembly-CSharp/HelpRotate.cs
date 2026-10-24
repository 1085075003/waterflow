using System;
using UnityEngine;
using UnityEngine.UI;

public class HelpRotate : MonoBehaviour
{
    public Animator fadeOut;
    public bool checkSwipe = true;
    public GameObject Rotation;
    public Text texto1a, texto1b, texto2a, texto2b;
    private string idioma, texto1, texto2;

    void Start() { checkSwipe = true; doLanguage(); }
    void Update() { CheckRotation(); }
    public void CheckRotation()
    {
        if (!checkSwipe || !SwipeHandler2.girado) return;
        if (fadeOut != null) fadeOut.enabled = true;
        PlayerPrefs.SetInt("helpRotation", 1);
        checkSwipe = false;
        if (Rotation != null) Rotation.SetActive(false);
    }
    public void ResetTutorial()
    {
        checkSwipe = true;
        doLanguage();
        if (Rotation != null) Rotation.SetActive(true);
        // A reused Unity scene needs the authored disabled first frame again.
        if (fadeOut != null)
        {
            fadeOut.enabled = true;
            fadeOut.Rebind();
            fadeOut.Update(0);
            fadeOut.enabled = false;
        }
    }
    public void doLanguage()
    {
        idioma = PlayerPrefs.GetString("language", "en");
        texto1 = ObtenerTexto1(); texto2 = ObtenerTexto2();
        if (idioma == "ar") { texto1 = Reverse(texto1); texto2 = Reverse(texto2); }
        SetText(texto1a, texto1); SetText(texto1b, texto2);
        SetText(texto2a, texto1); SetText(texto2b, texto2);
    }
    static string Reverse(string text) { char[] chars = text.ToCharArray(); Array.Reverse(chars); return new string(chars); }
    static void SetText(Text text, string value) { if (text != null) text.text = value; }
    public string ObtenerTexto1() { return NativeText(idioma ?? PlayerPrefs.GetString("language", "en"), false); }
    private string ObtenerTexto2() { return NativeText(idioma ?? PlayerPrefs.GetString("language", "en"), true); }
    // Return literals from original HelpRotate.ObtenerTexto1/2; unknown languages
    // follow the original English fallback, including the Arabic reverse rule.
    static string NativeText(string language, bool second)
    {
        switch (language)
        {
            case "de": return second ? "hier wischen" : "brett drehen";
            case "el": return second ? "σύρετε εδώ" : "περιστροφή πίνακα";
            case "es": return second ? "desliza aquí" : "girar tablero";
            case "fr": return second ? "glisser ici" : "tourner le plateau";
            case "hi": return second ? "यहाँ स्वाइप करें" : "बोर्ड घुमाएं";
            case "id": return second ? "geser di sini" : "putar papan";
            case "it": return second ? "scorri qui" : "ruota tavola";
            case "ja": return second ? "ここでスワイプ" : "ボードを回転";
            case "ko": return second ? "여기에서 스와이프" : "보드 회전";
            case "nl": return second ? "veg hier" : "draai bord";
            case "pl": return second ? "przesuń tutaj" : "obróć tablicę";
            case "pt": return second ? "deslize aqui" : "girar tabuleiro";
            case "ru": return second ? "проведите здесь" : "повернуть доску";
            case "sv": return second ? "svepa här" : "rotera brädet";
            case "th": return second ? "ปัดที่นี่" : "หมุนกระดาน";
            case "tr": return second ? "burada kaydır" : "tahtayı döndür";
            case "vi": return second ? "vuốt ở đây" : "xoay bảng";
            case "zh": return second ? "在这里滑动" : "旋转棋盘";
            case "zh-TW": return second ? "在這裡滑動" : "旋轉棋盤";
            default: return second ? "swipe here" : "rotate board";
        }
    }
}
