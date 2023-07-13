using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Rogue
{
    [Serializable]
    public struct SymbolsTextureData
    {
        //Ссылка на атлас шрифта
        public Texture texture;
        //Массив набора символов по порядку, начиная с левого-верхнего угла
        public char[] chars;

        //Dictionary с координатами каждого символа - номер строки и столбца
        private Dictionary<char, Vector2> charsDict;

        public void Initialize()
        {
            charsDict = new Dictionary<char, Vector2>();
            for (int i = 0; i < chars.Length; i++)
            {
                var c = char.ToLowerInvariant(chars[i]);
                if (charsDict.ContainsKey(c)) continue;
                //Расчет координат символа, преобразуем порядковый номер символа
                //в номер строки и столбца, зная, что длина строки равна 10.
                var uv = new Vector2(i % 10, 9 - i / 10);
                charsDict.Add(c, uv);
            }
        }

        public Vector2 GetTextureCoordinates(char c)
        {
            c = char.ToLowerInvariant(c);
            if (charsDict == null) Initialize();

            if (charsDict.TryGetValue(c, out Vector2 texCoord))
                return texCoord;
            return Vector2.zero;
        }
    }

    public interface ITextService {
        void Show(Vector3 position, string message, Color color, float? startSize = null);
    }
    [RequireComponent(typeof(ParticleSystem))]
    public class FloatingTextRenderService : MonoBehaviour, ITextService
    {
        public SymbolsTextureData textureData;        
        
        private ParticleSystemRenderer particleSystemRenderer;
        private new ParticleSystem particleSystem;
        public string text;
        [ContextMenu("TestText")]
        public void TestText()
        {
            Show(transform.position, text, Color.green);
        }
        
        public void SpawnParticle(Vector3 position, float amount, Color color, float? startSize = null)
        {
            var amountInt = Mathf.RoundToInt(amount);
            if (amountInt == 0) return;
            var str = amountInt.ToString();
            if (amountInt > 0) str = "+" + str;
            Show(position, str, color, startSize);
        }

        public void Show(Vector3 position, string message, Color color, float? startSize = null)
        {
            Span<Vector2> texCords = stackalloc Vector2[24]; //массив из 24 элемент - 23 символа + длина сообщения
            var messageLenght = Mathf.Min(23, message.Length);
            texCords[^1] = new Vector2(0, messageLenght);
            for (int i = 0; i < messageLenght; i++)
            {
                //if (i >= messageLenght) break;
                //Вызываем метод GetTextureCoordinates() из SymbolsTextureData для получения позиции символа
                texCords[i] = textureData.GetTextureCoordinates(message[i]);
            }

            var custom1Data = CreateCustomData(ref texCords);
            var custom2Data = CreateCustomData(ref texCords, 12);
            //Кэшируем ссылку на ParticleSystem
            if (particleSystem == null) particleSystem = GetComponent<ParticleSystem>();
            if (particleSystemRenderer == null)
            {
                //Если ссылка на ParticleSystemRenderer, кэшируем и убеждаемся в наличии нужных потоков
                particleSystemRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                var streams = new List<ParticleSystemVertexStream>();
                particleSystemRenderer.GetActiveVertexStreams(streams);
                //Добавляем лишний поток Vector2(UV2, SizeXY, etc.), чтобы координаты в скрипте соответствовали координатам в шейдере
                if (!streams.Contains(ParticleSystemVertexStream.UV2)) streams.Add(ParticleSystemVertexStream.UV2);
                if (!streams.Contains(ParticleSystemVertexStream.Custom1XYZW)) streams.Add(ParticleSystemVertexStream.Custom1XYZW);
                if (!streams.Contains(ParticleSystemVertexStream.Custom2XYZW)) streams.Add(ParticleSystemVertexStream.Custom2XYZW);
                particleSystemRenderer.SetActiveVertexStreams(streams);
            }

            //Инициализируем параметры эммишена
            //Цвет и позицию получаем из параметров метода
            //Устанавливаем startSize3D по X, чтобы символы не растягивались и не сжимались
            //при изменении длины сообщения
            var emitParams = new ParticleSystem.EmitParams
            {
                startColor = color,
                position = position,
                applyShapeToPosition = true,
                startSize3D = new Vector3(messageLenght, 1, 1)
            };
            //Если мы хотим создавать частицы разного размера, то в параметрах SpawnParticle неоходимо
            //передать нужное значение startSize
            if (startSize.HasValue) emitParams.startSize3D *= startSize.Value * particleSystem.main.startSizeMultiplier;
            //Непосредственно спаун частицы
            particleSystem.Emit(emitParams, 1);

            //Передаем кастомные данные в нужные потоки
            //var data = new NativeList<Vector4>();
            //Получаем поток ParticleSystemCustomData.Custom1 из ParticleSystem
            var customData = new List<Vector4>();
            particleSystem.GetCustomParticleData(customData, ParticleSystemCustomData.Custom1);

            //var last = customData.Count > 1 ? customData.Count - 1 : 0;
            //Debug.Log(last);
            //Меняем данные последнего элемент, т.е. той частицы, которую мы только что создали

            customData[customData.Count - 1] = custom1Data;
            //Возвращаем данные в ParticleSystem
            //Возвращаем данные в ParticleSystem
            particleSystem.SetCustomParticleData(customData, ParticleSystemCustomData.Custom1);

            //Аналогично для ParticleSystemCustomData.Custom2
            particleSystem.GetCustomParticleData(customData, ParticleSystemCustomData.Custom2);
            customData[customData.Count - 1] = custom2Data;
            particleSystem.SetCustomParticleData(customData, ParticleSystemCustomData.Custom2);
        }

        //Функция упаковки массива Vector2 с координатами символов во float
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float PackFloat(in Span<Vector2> vecs, int lenght)
        {
            if (lenght == 0) return 0;
            //Поразрядно добавляем значения координат векторов в float
            var result = vecs[0].y * 10000 + vecs[0].x * 100000;
            if (lenght > 1) result += vecs[1].y * 100 + vecs[1].x * 1000;
            if (lenght > 2) result += vecs[2].y + vecs[2].x * 10;
            return result;
        }

        //Функция создания Vector4 для потока с CustomData
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector4 CreateCustomData(ref Span<Vector2> texCoords, int offset = 0)
        {
            var data = Vector4.zero;
            Span<Vector2> vecs = stackalloc Vector2[3];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var ind = i * 3 + j + offset;
                    if (texCoords.Length > ind)
                    {
                        vecs[j] = texCoords[ind];
                    }
                    else
                    {
                        data[i] = PackFloat(in vecs, 3);
                        i = 5;
                        break;
                    }
                }
                if (i < 4) data[i] = PackFloat(in vecs, 3);
            }
            return data;
        }
    }
}
