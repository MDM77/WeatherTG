using Newtonsoft.Json;
using System.Text;
using WeatherTG;
using System.Web;

Console.OutputEncoding = Encoding.UTF8;
var weatherKey = "de652bea5fc39e262e384811c44b47a7";
var tgKey = "6058344620:AAFaxZxt9C2NkDmYs15dwOER9xEy5-gyXSI";
var offsetTG = 0;
var client = new HttpClient();

var messageTG = "";
var idTG = 0;
var botResponse = "";

while (true)
{
    var responseTG = await client.GetAsync(@$"https://api.telegram.org/bot{tgKey}/getUpdates?offset={offsetTG}");
    var result = await responseTG.Content.ReadAsStringAsync();
    var model = JsonConvert.DeserializeObject<ROTGBot>(result);

    if (model.result.Length > 0)
    {
        offsetTG = model.result[^1].update_id + 1;
    }

    if (responseTG.IsSuccessStatusCode)
    {
        foreach (var t in model.result)
        {
            messageTG = t.message.text;
            idTG = t.message.chat.id;

            if (messageTG == "/start")
            {
                botResponse = $"Привет! Я могу показать тебе небольшую погодную сводку, просто напиши название города";
                var botResponseAsync = await client.GetAsync($"https://api.telegram.org/bot{tgKey}/sendMessage?chat_id={t.message.chat.id}&text={botResponse}");
            }
            else
            {
                var responseWeather = await client.GetAsync(@$"https://api.openweathermap.org/data/2.5/forecast?q={messageTG}&appid={weatherKey}&units=metric&lang=ru");
                if (responseWeather.IsSuccessStatusCode)
                {
                    var resultWeather = await responseWeather.Content.ReadAsStringAsync();
                    var modelWeather = JsonConvert.DeserializeObject<ROWeather>(resultWeather);

                    botResponse = ($"{DateTime.Now}\n" +
                                  $"В городе {modelWeather.city.name} {modelWeather.list[0].weather[0].description}.\n" +
                                  $"Температура: {Math.Round(modelWeather.list[0].main.temp, 1)}.\n" +
                                  $"Ветер: {modelWeather.list[0].wind.speed}м/с, {WindDirection(modelWeather.list[0].wind.deg)}\n" +
                                  $"Влажность: {modelWeather.list[0].main.humidity}%\n" +
                                  $"Давление: {modelWeather.list[0].main.grnd_level}");
                    var botResponseAsync = await client.GetAsync($"https://api.telegram.org/bot{tgKey}/sendMessage?chat_id={t.message.chat.id}&text={botResponse}");
                }
                else
                {
                    botResponse = "Я не смог найти такой город, попробуй ещё раз.";
                    var botResponseAsync = await client.GetAsync($"https://api.telegram.org/bot{tgKey}/sendMessage?chat_id={t.message.chat.id}&text={botResponse}");
                }
            }
        }
    }

    string WindDirection(int wind) =>
            wind switch
            {
                >= 0 and < 23 or >= 338 and <= 360 => "C",
                >= 23 and < 68 => "СВ",
                >= 68 and < 113 => "В",
                >= 113 and < 158 => "ЮВ",
                >= 158 and < 203 => "Ю",
                >= 203 and < 248 => "ЮЗ",
                >= 248 and < 292 => "З",
                >= 292 and < 338 => "CЗ",
                _ => "",
            };
}