using UnityEngine;
using UnityEngine.UI;

namespace GameJam2026
{
    public sealed class ManualNavigation : MonoBehaviour
    {
        [SerializeField] private Image pageImage;
        [SerializeField] private Sprite[] pages;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;

        private int currentPage;

        private void Awake()
        {
            currentPage = 0;
            RefreshPage();
        }

        public void PreviousPage()
        {
            if (currentPage <= 0) return;
            currentPage--;
            RefreshPage();
        }

        public void NextPage()
        {
            if (pages == null || currentPage >= pages.Length - 1) return;
            currentPage++;
            RefreshPage();
        }

        private void RefreshPage()
        {
            bool hasPages = pages != null && pages.Length > 0;
            pageImage.sprite = hasPages ? pages[currentPage] : null;
            previousButton.interactable = hasPages && currentPage > 0;
            nextButton.interactable = hasPages && currentPage < pages.Length - 1;
        }
    }
}
