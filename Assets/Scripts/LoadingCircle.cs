using UnityEngine;
using UnityEngine.UI;

public class LoadingCircle : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    
    public void SetFillAmount(float amount) {
        if(_fillImage != null) {
            _fillImage.fillAmount = Mathf.Clamp01(amount);
        }
    }
}