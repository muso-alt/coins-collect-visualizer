using System;
using TMPro;
using UnityEngine;
using DG.Tweening;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Runtime.Services
{
    public class CoinCollectionVisualizer
    {
        private readonly Transform _moveItemsParent;
        private readonly CancellationTokenSource _tokenSource;
        private readonly Transform _item;
        private readonly List<Transform> _coinsPool = new();
        
        private int _totalCurrency;
        
        //Default values set
        private float _coinsShowInterval = .1f;
        private float _coinStartScale = .6f;
        private float _coinsEndScale = .8f;
        private float _coinFlyDuration = .6f;
        private float _localMoveDuration = .3f;
        
        public CoinCollectionVisualizer(Transform itemParent, Transform item, int totalCurrency = 0)
        {
            _moveItemsParent = itemParent;
            _totalCurrency = totalCurrency;
            _item = item;
            
            _tokenSource = new CancellationTokenSource();
        }

        public void InitializeParameters(float showInterval, float startScale,
            float endScale, float flyDuration, float localMoveDuration)
        {
            _coinsShowInterval = showInterval;
            _coinStartScale = startScale;
            _coinsEndScale = endScale;
            _coinFlyDuration = flyDuration;
            _localMoveDuration = localMoveDuration;
        }
        
        public async UniTask VisualizeCoinsCollectAsync(Transform endPosition, Vector2 createPosition, int coins, TMP_Text textToUpdate = null)
        {
            if (coins <= 0)
            {
                return;
            }

            var tasks = new List<UniTask>();

            const float totalDelay = 1;
            
            var toTwo = .15f;

            var k = totalDelay * coins / (coins * (coins + 1) / 2f);
            
            for (var i = 0; i < coins; i++)
            {
                if (i != 0)
                {
                    var delay = k / i;

                    await UniTask.Delay(TimeSpan.FromSeconds(delay),
                        cancellationToken: _tokenSource.Token);
                }
                else if (coins == 2)
                {
                    //HACK
                    await UniTask.Delay(TimeSpan.FromSeconds(toTwo *= _coinsShowInterval),
                        cancellationToken: _tokenSource.Token);
                }

                var sequence = DOTween.Sequence();

                var coin = GetCoin();

                coin.gameObject.SetActive(true);
                coin.localScale = Vector3.one * _coinStartScale;
                
                coin.position = createPosition;

                var randomJump = Random.Range(-3.5f, -2);
                var localPosition = coin.localPosition;
                var randomX = Random.Range(localPosition.x - 100, localPosition.x + 100);
                var randomY = Random.Range(localPosition.y - 30f, localPosition.y - 35f);
                
                coin.DOScale(_coinsEndScale, 
                    _coinFlyDuration + _localMoveDuration);

                sequence.Append(coin.DOLocalMove(new Vector2(randomX, randomY), _localMoveDuration)
                    .SetEase(Ease.OutSine));

                sequence.Append(coin
                    .DOJump(endPosition.position, randomJump, 1,
                        _coinFlyDuration).SetEase(Ease.InQuad));

                if (i < (coins < 3 ? coins : coins - 1))
                {
                    tasks.Add(sequence.AsyncWaitForCompletion().AsUniTask());
                }

                sequence.OnComplete(() =>
                {
                    _totalCurrency++;
                    
                    _totalCurrency = Mathf.Clamp(_totalCurrency, 0, _totalCurrency);
                    
                    if(textToUpdate != null)
                    {
                        textToUpdate.text = _totalCurrency.ToString();
                    }
                    
                    coin.gameObject.SetActive(false);
                    _coinsPool.Add(coin);
                });
            }

            await UniTask.WhenAll(tasks);
        }

        private Transform GetCoin()
        {
            if (_coinsPool.Count > 0)
            {
                var coin = _coinsPool[0];
                _coinsPool.Remove(coin);
                return coin;
            }
            
            return Object.Instantiate(_item, _moveItemsParent);
        }

        public void Dispose()
        {
            foreach (var coin in _coinsPool)
            {
                Object.Destroy(coin.gameObject);
            }
        }
    }
}
