using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using AxuFrameKit._Core.Utility.RandomX;
using AxuFrameKit.Common.Utility.Net;

namespace TestAndSample.Sample_Core.Utility.RandomX
{
    public class RandomAvatarDownload : MonoBehaviour
    {
        public Image imgPrefab;

        public int count;

        private async void Start()
        {
            for (int i = 0; i < count; i++)
            {
                switch (UnityEngine.Random.Range(0, 3))
                {
                    case 0:
                        LoadRobot(Instantiate(imgPrefab, imgPrefab.transform.parent).GetComponent<Image>());
                        break;
                    case 1:
                        LoadHuman(Instantiate(imgPrefab, imgPrefab.transform.parent).GetComponent<Image>());
                        break;
                    case 2:
                        LoadPixel(Instantiate(imgPrefab, imgPrefab.transform.parent).GetComponent<Image>());
                        break;
                }

                await Task.Delay(TimeSpan.FromSeconds(0.2f));
            }
        }

        // private void Update()
        // {

        //     if (Input.GetKeyDown(KeyCode.R))
        //     {
        //         LoadRobot();
        //     }

        //     if (Input.GetKeyDown(KeyCode.H))
        //     {
        //         LoadHuman();
        //     }

        //     if (Input.GetKeyDown(KeyCode.P))
        //     {
        //         LoadPixel();
        //     }
        // }

        private async void LoadRobot(Image image)
        {
            string url = RandomAvatarUrl.GetRobot();

            image.sprite = await Downloader.DownloadSpriteAsync(url, 100f);
        }

        private async void LoadHuman(Image image)
        {
            string url = RandomAvatarUrl.GetHuman((RandomAvatarUrl.HumanType)UnityEngine.Random.Range(0, 3));

            image.sprite = await Downloader.DownloadSpriteAsync(url, 100f);
        }

        private async void LoadPixel(Image image)
        {
            string url = RandomAvatarUrl.GetPixel();

            image.sprite = await Downloader.DownloadSpriteAsync(url, 100f);
        }
    }
}