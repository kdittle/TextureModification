using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextToTexture : MonoBehaviour
{

    [Tooltip("Texture to pull the font data from.")]
    public Texture2D FontTexture;

    [Tooltip("Number of colums in the font texture.")]
    public int fontCountX;

    [Tooltip("Number of rows in the font texture.")]
    public int fontCountY;

    [Tooltip("The width of each grid cell in the font texture.")]
    public float boundingBoxX;

    [Tooltip("The height of each grid cell in the font texture.")]
    public float boundingBoxY;

    [Tooltip("The x coordinate on the texture to place the selected font item.")]
    public int textPlacementX;

    private int modifiedTextPlacementX;

    [Tooltip("The y coordinate on the texture to place the selected font item.")]
    public int textPlacementY;

    private int modifiedTextPlacementY;

    [Tooltip("Size of the decal texture. Used for the font.")]
    public int decalTextureSize = 1024;

    [Tooltip("Offset on the x coordinate. Used for spacing out multiple characters.")]
    public float charXoffset = 25.0f;

    private float modifiedCharXoffset;

    private Texture originalTexture;
    private string currentNumber = null;

    [Tooltip("Checking this will write to the texture at scene loading.")]
    public bool InitOnLoad;

    [System.Serializable]
    public struct BoundingBox
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public BoundingBox(float _x, float _y, float _width, float _height)
        {
            x = _x;
            y = _y;
            width = _width;
            height = _height;
        }
    }

    public List<BoundingBox> TargetFontRenderAreas;

    protected string jerseyNumber;
    protected List<char> SeparatedNumber;

    //Texture the font is going to be applied to.
    private const string TEXT_TEXTURE_ID = "_MainTex";

    private Material materialToEdit; //The material that is going to have the text applied to
    private Texture2D perservedMatTexture;  //Texture2D holder for the un-editted material

    //Boudning boxes for each number
    private Dictionary<int, List<float>> CharacterBoundingBox;

    private Texture finalTexture;

    // Use this for initialization
    void Start()
    {
        if (InitOnLoad)
            Init();
    }

    public void Init()
    {
        if (GetComponent<Renderer>().material.mainTexture != null)
        {
            modifiedTextPlacementX = textPlacementX;
            modifiedTextPlacementY = textPlacementY;
            modifiedCharXoffset = charXoffset;

            //Create the dictionary that holds the bounding box info for the number grid
            CharacterBoundingBox = new Dictionary<int, List<float>>()
            {
                //index of the number box, box (x, y, width, height)
                {0, new List<float>() {0, boundingBoxY, boundingBoxX, boundingBoxY}}, //1
                {1, new List<float>() {boundingBoxX, boundingBoxY, boundingBoxX, boundingBoxY}}, //2
                {2, new List<float>() {boundingBoxX * 2, boundingBoxY, boundingBoxX, boundingBoxY}}, //3
                {3, new List<float>() {boundingBoxX * 3, boundingBoxY, boundingBoxX, boundingBoxY}}, //4
                {4, new List<float>() {boundingBoxX * 4, boundingBoxY, boundingBoxX, boundingBoxY}}, //5
                {5, new List<float>() {0, 0, boundingBoxX, boundingBoxY}}, //6
                {6, new List<float>() {boundingBoxX, 0, boundingBoxX, boundingBoxY}}, //7
                {7, new List<float>() {boundingBoxX * 2, 0, boundingBoxX, boundingBoxY}}, //8
                {8, new List<float>() {boundingBoxX * 3, 0, boundingBoxX, boundingBoxY}}, //9
                {9, new List<float>() {boundingBoxX * 4, 0, boundingBoxX, boundingBoxY}}, //0
            };

            originalTexture = GetComponent<Renderer>().material.mainTexture;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            NextNumber();
    }

    private void NextNumber()
    {
        GetComponent<Renderer>().material.mainTexture = originalTexture;

        modifiedTextPlacementX = textPlacementX;
        modifiedTextPlacementY = textPlacementY;
        modifiedCharXoffset = charXoffset;

        if (currentNumber == null)
            currentNumber = "00";
        else if (currentNumber == "00")
            currentNumber = "0";
        else if (currentNumber == "99")
            currentNumber = "00";
        else
        {
            int num = int.Parse(currentNumber);
            num++;
            currentNumber = num.ToString();
        }

        if (currentNumber.Length == 1)
            modifiedTextPlacementX += 60;
        else if (currentNumber == "00")
        {
            modifiedTextPlacementX -= 10;
            modifiedCharXoffset -= 0;
        }
        else if (currentNumber.Length == 2 && currentNumber[0] == '1')
        {
            modifiedTextPlacementX += 5;
            modifiedCharXoffset -= 25;
        }
        else
        {
            modifiedTextPlacementX += 0;
            //modifiedCharXoffset -= 5;
        }

        jerseyNumber = currentNumber;

        SeparatedNumber = new List<char>(jerseyNumber.ToCharArray());

        DrawToTexture();
    }

    #region Blanket-Standard Spacing
    /*
if(currentNumber.Length == 1)
    modifiedTextPlacementX += 60;
else if (currentNumber == "00")
{
    modifiedTextPlacementX += 0;
    modifiedCharXoffset -= 10;
}
else if (currentNumber.Length == 2 && currentNumber[0] == '1')
{
        modifiedTextPlacementX += 5;
        modifiedCharXoffset -= 40;
}
else
{
    modifiedTextPlacementX += 0;
    //modifiedCharXoffset -= 5;
}
*/
    #endregion

    private void DrawToTexture()
    {
        Material[] mats = GetComponent<Renderer>().materials;

        //Get the material to edit.
        materialToEdit = mats[0];

        //Store the material before it gets changed
        perservedMatTexture = (Texture2D)materialToEdit.mainTexture;
        materialToEdit.SetTexture(TEXT_TEXTURE_ID, CreateTextToTexture());
    }

    /// <summary>
    /// Creates a texture to be added to a texture.
    /// </summary>
    /// <returns></returns>
    private Texture2D CreateTextToTexture()
    {
        //Need a new texture to add things to
        //Create a clear texture
        Texture2D createdFontTexture = CreateClearTexture2D(Color.clear, decalTextureSize, decalTextureSize);

        //Set important properties, just to make sure the text applies properly
        createdFontTexture.filterMode = FilterMode.Bilinear;
        createdFontTexture.wrapMode = TextureWrapMode.Clamp;

        Vector2 characterPosition;
        Color[] characterPixels;
        char character;

        //for (int b = 0; b < TargetFontRenderAreas.Count; b++)
        for (int b = 0; b < 1; b++)
        {
            float xOffset = 0;
            for (int i = 0; i < SeparatedNumber.Count; i++)
            {
                //Get the character we are looking for
                character = SeparatedNumber[i];

                if (i > 0)
                    xOffset += modifiedCharXoffset; //If we are working on the second number, we need to move it over

                //Move the offset of solo numbers
                //if (SeparatedNumber.Count == 1)
                xOffset += modifiedTextPlacementX;

                //Get the character's position in the font grid
                characterPosition = GetCharacterGridPosition(character);

                //Get the pixels at the given grid position
                characterPixels = FontTexture.GetPixels((int)characterPosition.x, (int)characterPosition.y,
                    (int)boundingBoxX, (int)boundingBoxY);

                //Add the pixels we just got to the created material
                //createdFontTexture = AddPixelsToTexture(createdFontTexture, characterPixels, modifiedTextPlacementX + (int) xOffset, textPlacementY, (int) boundingBoxX, (int) boundingBoxY);
                createdFontTexture = AddPixelsToTexture(createdFontTexture, characterPixels,
                    (int)TargetFontRenderAreas[b].x + (int)xOffset,
                    (int)TargetFontRenderAreas[b].y, (int)TargetFontRenderAreas[b].width,
                    (int)TargetFontRenderAreas[b].height);
            }
        }

        //Apply changes to the created material
        createdFontTexture.Apply();

        //Create a texture that will merge the original texture and the font texture together
        Texture2D mergedTextures = CreateClearTexture2D(Color.clear, perservedMatTexture.width, perservedMatTexture.height);

        mergedTextures = MergeTextures(perservedMatTexture, createdFontTexture, TargetFontRenderAreas[0]);

        //Apply changes to the newly created texture
        mergedTextures.Apply();

        return mergedTextures;
    }

    /// <summary>
    /// Creates a clear texture of given width and height.
    /// </summary>
    /// <param name="_color"></param>
    /// <param name="_width"></param>
    /// <param name="_height"></param>
    /// <returns></returns>
    private Texture2D CreateClearTexture2D(Color _color, int _width, int _height)
    {
        //Debug.Log("Creating clear texture.");
        Texture2D newTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        int numPixels = _width * _height;
        Color[] colors = new Color[numPixels];

        for (int i = 0; i < numPixels; i++)
        {
            colors[i] = _color;
        }
        newTexture.SetPixels(colors);

        newTexture.Apply();

        return newTexture;
    }

    /// <summary>
    /// Retrieve the character's position given the bounding box information.
    /// </summary>
    /// <param name="_character"></param>
    /// <returns></returns>
    private Vector2 GetCharacterGridPosition(char _character)
    {
        Vector2 pos = Vector2.zero;

        //This is a somewhat hard coded method since the font texture is a custom created item.
        //It also only takes into account the idea that there are 10 characters in the font texture

        switch (_character)
        {
            case '1':
            {
                for (int i = 0; i < CharacterBoundingBox[0].Count; i++)
                {
                    if (i == 0)
                        pos.x = CharacterBoundingBox[0][i];
                    if (i == 1)
                        pos.y = CharacterBoundingBox[0][i];
                }
                break;
            }
            case '2':
            {
                for (int i = 0; i < CharacterBoundingBox[1].Count; i++)
                {
                    if (i == 0)
                        pos.x = CharacterBoundingBox[1][i];
                    if (i == 1)
                        pos.y = CharacterBoundingBox[1][i];
                }
                break;
            }
            case '3':
            {
                for (int i = 0; i < CharacterBoundingBox[2].Count; i++)
                {
                    if (i == 0)
                        pos.x = CharacterBoundingBox[2][i];
                    if (i == 1)
                        pos.y = CharacterBoundingBox[2][i];
                }
                break;
            }
            case '4':
            {
                for (int i = 0; i < CharacterBoundingBox[3].Count; i++)
                {
                    if (i == 0)
                        pos.x = CharacterBoundingBox[3][i];
                    if (i == 1)
                        pos.y = CharacterBoundingBox[3][i];
                }
                break;
            }
            case '5':
            {
                for (int i = 0; i < CharacterBoundingBox[4].Count; i++)
                {
                    if (i == 0)
                        pos.x = CharacterBoundingBox[4][i];
                    if (i == 1)
                        pos.y = CharacterBoundingBox[4][i];
                }
                break;
            }
            case '6':
            {
                for (int i = 0; i < CharacterBoundingBox[5].Count; i++)
                {
                    if (i == 0)
                        pos.x = CharacterBoundingBox[5][i];
                    if (i == 1)
                        pos.y = CharacterBoundingBox[5][i];
                }
                break;
            }
            case '7':
            {
                for (int i = 0; i < CharacterBoundingBox[6].Count; i++)
                {
                    if (i == 0)
                        pos.x = CharacterBoundingBox[6][i];
                    if (i == 1)
                        pos.y = CharacterBoundingBox[6][i];
                }
                break;
            }
            case '8':
            {
                for (int i = 0; i < CharacterBoundingBox[7].Count; i++)
                {
                    if (i == 0)
                        pos.x = CharacterBoundingBox[7][i];
                    if (i == 1)
                        pos.y = CharacterBoundingBox[7][i];
                }
                break;
            }
            case '9':
            {
                for (int i = 0; i < CharacterBoundingBox[8].Count; i++)
                {
                    if (i == 0)
                        pos.x = CharacterBoundingBox[8][i];
                    if (i == 1)
                        pos.y = CharacterBoundingBox[8][i];
                }
                break;
            }
            case '0':
            {
                for (int i = 0; i < CharacterBoundingBox[9].Count; i++)
                {
                    if (i == 0)
                        pos.x = CharacterBoundingBox[9][i];
                    if (i == 1)
                        pos.y = CharacterBoundingBox[9][i];
                }
                break;
            }
        }

        return pos;
    }

    /// <summary>
    /// Add the pixels from the created font texture to the new one.
    /// </summary>
    /// <param name="_texture"></param>
    /// <param name="_pixels"></param>
    /// <param name="_posX"></param>
    /// <param name="_posY"></param>
    /// <param name="_width"></param>
    /// <param name="_height"></param>
    /// <returns></returns>
    private Texture2D AddPixelsToTexture(Texture2D _texture, Color[] _pixels, int _posX, int _posY, int _width, int _height)
    {
        //Debug.Log("Adding pixels to texture.");
        int pixelCount = 0;
        Color[] curPixels;

        if (_posX + _width <= _texture.width && _posY + _height <= _texture.height)
        {
            //Get the pixels from the grid box being looked at
            curPixels = _texture.GetPixels(_posX, _posY, _width, _height);

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    pixelCount = x + (y * _width);

                    if (curPixels[pixelCount].a >= .8f)
                    {
                        _pixels[pixelCount] = curPixels[pixelCount];
                    }
                }
            }
            _texture.SetPixels(_posX, _posY, _width, _height, _pixels);
        }
        else
        {
            if (_posX + _width > _texture.width)
                Debug.Log("Position + width is too large for texture.");
            else
                Debug.Log("Positiong + height is too large for texture.");
        }
        _texture.Apply();
        return _texture;
    }

    private Texture2D MergeTextures(Texture2D mainTexture, Texture2D detailTexture, BoundingBox targetRenderArea)
    {
        Texture2D newTexture = new Texture2D(mainTexture.width, mainTexture.height);
        int backgroundPixels = mainTexture.width * mainTexture.height;
        int pixelIndex = 0;

        Color[] mainColors = mainTexture.GetPixels();   //Holds the pixel array for the main texture
        Color[] detailColors = detailTexture.GetPixels();   //Holds the pixel array for the detail to be drawn to the main texture
        Color[] combinedColors = new Color[backgroundPixels];   //Holds the new pixel array

        //Height
        for (int y = 0; y < mainTexture.height; y++)
        {
            //Width
            for (int x = 0; x < mainTexture.width; x++)
            {
                //The index we are on
                pixelIndex = x + (y * mainTexture.width);

                //The background pixel we are looking at 
                Color backgroundPixel = mainColors[pixelIndex];

                //We only want to add colored and somewhat opaque pixels to the background texture
                if (pixelIndex < detailColors.Length && detailColors[pixelIndex].a >= 0.1f)
                    combinedColors[pixelIndex] = detailColors[pixelIndex];
                else
                    combinedColors[pixelIndex] = backgroundPixel;
            }
        }

        //Set all the pixels for the combined colors
        newTexture.SetPixels(combinedColors);
        newTexture.Apply();

        return newTexture;
    }

    private Texture2D ScaleTexture(Texture2D texture, int targetWidth, int targetHeight)
    {
        Texture2D newTexture = new Texture2D(targetWidth, targetHeight, texture.format, true);
        Color[] newPixels = newTexture.GetPixels(0);

        float incX = (1.0f / (float)targetWidth);
        float incY = (1.0f / (float)targetHeight);
        int scalePixelCount = 0;

        for (int pixelIndex = 0; pixelIndex < newPixels.Length; pixelIndex++)
        {
            //When rescalling, we really want less of the transparent and more of the opaque
            if (texture.GetPixelBilinear(incX * ((float)pixelIndex % targetWidth), incY * ((float)pixelIndex % targetHeight)).a >= .1f)
            {
                newPixels[pixelIndex] = texture.GetPixelBilinear(incX * ((float)pixelIndex % targetWidth), incY * ((float)pixelIndex % targetHeight));

                scalePixelCount++;
            }
        }

        //Debug.Log("Scale Pixel Count: " + scalePixelCount);
        newTexture.SetPixels(newPixels, 0);
        newTexture.Apply();

        return newTexture;
    }

    public void SetFontTexture(Texture2D texture)
    {
        FontTexture = texture;
    }
}
