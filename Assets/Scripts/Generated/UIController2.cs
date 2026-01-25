using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController2 : MonoBehaviour
{
    [SerializeField] private CanvasRenderer _imageCanvasRenderer;
    [SerializeField] private Image _image;

    [SerializeField] private CanvasRenderer _textTMPCanvasRenderer;
    [SerializeField] private TextMeshProUGUI _textTMP;

    [SerializeField] private CanvasRenderer _inputFieldTMPCanvasRenderer;
    [SerializeField] private Image _inputFieldTMP;
    [SerializeField] private TMP_InputField _inputFieldTMPInputField;

    [SerializeField] private CanvasRenderer _scrollViewCanvasRenderer;
    [SerializeField] private Image _scrollView;
    [SerializeField] private ScrollRect _scrollViewScrollRect;
}