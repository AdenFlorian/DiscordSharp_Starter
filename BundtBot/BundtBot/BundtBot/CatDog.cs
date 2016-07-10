﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;

namespace BundtBot.BundtBot {
    public static class CatDog {
        public static async Task Cat(CommandEventArgs e) {
            var rand = new Random();
            if (rand.NextDouble() >= 0.5) {
                using (var webclient = new HttpClient()) {
                    var s = await webclient.GetStringAsync("http://random.cat/meow");
                    var pFrom = s.IndexOf("\\/i\\/", StringComparison.Ordinal) + "\\/i\\/".Length;
                    var pTo = s.LastIndexOf("\"}", StringComparison.Ordinal);
                    var cat = s.Substring(pFrom, pTo - pFrom);
                    Console.WriteLine("http://random.cat/i/" + cat);
                    await e.Channel.SendMessage("I found a cat\nhttp://random.cat/i/" + cat);
                }
            } else {
                await Dog(e, "how about a dog instead");
            }
        }

        public static async Task Dog(CommandEventArgs e, string message) {
            try {
                using (var client = new HttpClient()) {
                    client.Timeout = new TimeSpan(2000);
                    client.BaseAddress = new Uri("http://random.dog");
                    var dog = await client.GetStringAsync("woof");
                    Console.WriteLine("http://random.dog/" + dog);
                    await e.Channel.SendMessage(message + "\nhttp://random.dog/" + dog);
                }
            } catch (Exception) {
                await e.Channel.SendMessage("there are no dogs here, who let them out (random.dog is down :dog: :interrobang:)");
            }
        }
    }
}