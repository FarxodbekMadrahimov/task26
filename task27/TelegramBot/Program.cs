using Newtonsoft.Json;
using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot;
using Telegram.Bot.Args;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


string YOUR_ACCESS_TOKEN_HERE = "{YOUR_ACCESS_TOKEN_HERE}";
var botClient = new TelegramBotClient($"{YOUR_ACCESS_TOKEN_HERE}");

using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{

    var handler = update.Type switch
    {
        UpdateType.Message => HandleMessageAsync(botClient, update, cancellationToken),
        UpdateType.CallbackQuery => HandleCallBackQueryAsync(botClient, update, cancellationToken),
    };
    try
    {
        await handler;
    }
    catch
    {
        await Console.Out.WriteLineAsync("XATO");
    }

}

static async Task HandleMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;


    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
    ////////////////////////////////////////////////////////////////////////////
    ///bu yerda Json formatdagi malumotni Obyektga og'irish jarayoni amalga oshiriladi va Listga qo'shib qo'yiladi
    string x = messageText;
    //Console.WriteLine(x);
    //   Console.WriteLine("Qidirayotgan narsangizni kiriting :");
    string Natijalar;
    List<string> NatijalarListi = new List<string>();
    // string x = Console.ReadLine();
    string str = "[";
    for (int i = 0; i < x.Length; i++)
    {
        if (!str.Contains(x[i].ToString())) str = str + x[i].ToString();
    }
    str += "]";

    using (HttpClient client = new HttpClient())
    {

        string BaseUrl = "https://api.publicapis.org/entries";
        HttpResponseMessage proces = await client.GetAsync(BaseUrl);
        string JsonResult = await proces.Content.ReadAsStringAsync();
        CountAndApi APIss = JsonConvert.DeserializeObject<CountAndApi>(JsonResult);
        //Console.WriteLine(JsonResult);

        foreach (var item in APIss.entries)
        {
            if (Regex.IsMatch(item.API, str))
            {
                NatijalarListi.Add(item.Link);
            }
        }
    }
    /////////////////////////////////////////////////////////////////////////////////////////
    /// KeyingiButton oldinga yoki orqaga harakatni ko'rsatadi
    /// sanoqchi bu 10 ta yoki undan kamroq buttonlar bilan ishlaydigan oynamizda 1...10 gacha sanaydi
    /// ekrangaChiquvchiMalumot bu  foydalanuvchiga 10 tagacha nimalar borligini ko'rsatib beradi , bu tepadagi sanovchiga bogliq
    Random rnd = new Random();
    int KeyingiButton = rnd.Next(NatijalarListi.Count);
    int sanoqchi = 0;
    string ekeangaChiquvchiMalumot = "";
    /////////////////////////////////////////////////////////////////////////////////////////////// 
    ///Malumotlar qoshish
    /// oynada hosil qilinayotgan buttonlar ketma - ketligini tuzib chiqish  
    List<List<InlineKeyboardButton>> KopQatorButtonlar = new List<List<InlineKeyboardButton>>();
    for (int i = 0; i < 2; i++)
    {
        List<InlineKeyboardButton> qatorButtonlar = new List<InlineKeyboardButton>();
        for (int j = 0; j < 5; j++)
        {
            if (KeyingiButton * 10 + sanoqchi < NatijalarListi.Count)
            {
                qatorButtonlar.Add(InlineKeyboardButton.WithCallbackData(text: (sanoqchi + 1).ToString(), callbackData: NatijalarListi[KeyingiButton * 10 + sanoqchi]));
                ekeangaChiquvchiMalumot = ekeangaChiquvchiMalumot + String.Format("{0}.{1}\n", 1 + sanoqchi, NatijalarListi[KeyingiButton * 10 + sanoqchi]);
                sanoqchi++;
            }
        }
        KopQatorButtonlar.Add(qatorButtonlar);
    }

    List<InlineKeyboardButton> harakatButtonlar = new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData(text: "<-", callbackData: "<-"),
            InlineKeyboardButton.WithCallbackData(text: "x", callbackData: "x"),
            InlineKeyboardButton.WithCallbackData(text: "->", callbackData: "->"),
        };

   
    KopQatorButtonlar.Add(harakatButtonlar);
    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(KopQatorButtonlar);
    Message sentMessage = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: ekeangaChiquvchiMalumot,
        replyMarkup: inlineKeyboard,
        cancellationToken: cancellationToken);



}

static async Task HandleCallBackQueryAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    string? xabarniJonat = update.CallbackQuery.Data;
    //  Console.WriteLine("Bu yerda javob {0}", xabarniJonat);
    if (xabarniJonat.Equals("x") || xabarniJonat.Equals("->") || xabarniJonat.Equals("<-"))
        HandleMessageAsync(botClient, update, cancellationToken);
    else
    {
        Message sentMessage1 = await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.From.Id,
        text: String.Format("{0}", xabarniJonat),
        cancellationToken: cancellationToken);
    }

}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

