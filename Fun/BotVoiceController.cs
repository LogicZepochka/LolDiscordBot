using DSharpPlus.Entities;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.VoiceNext;
using System.IO;
using System.Diagnostics;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace LoLDiscover.Fun
{
    public static class BotVoiceController
    {
        private static async Task PrepareText(string text)
        {
            var config = SpeechConfig.FromSubscription("1873936db7c64c83a1d847ce400eb08c", "westus");
            config.SpeechSynthesisLanguage = "ru-RU";
            var audioConfig = AudioConfig.FromWavFileOutput("speak.wav");
            var langConf = AutoDetectSourceLanguageConfig.FromLanguages(new string[] { "ru-RU" });
            Console.WriteLine("Озвучивание текста: {0}", text);
            var synth = new SpeechSynthesizer(config, audioConfig);
            var result = await synth.SpeakTextAsync(text);

            if (result.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine("\n\n\nПроизошла ошибка при воспроизведении текста:\nПричина:{0}\n", result.Reason.ToString());

                if (result.Reason == ResultReason.Canceled)
                {
                    Console.WriteLine("\n{0}\n{1}\n{2}", SpeechSynthesisCancellationDetails.FromResult(result).Reason, SpeechSynthesisCancellationDetails.FromResult(result).ErrorCode, SpeechSynthesisCancellationDetails.FromResult(result).ErrorDetails);
                }
                return;
            }
            else
            {
                Console.Beep();
                Console.WriteLine("\nТекст распознан удачно и воспроизведен\n");
            }
            return;
        }

        private static async Task SpeakText(DiscordChannel chn)
        {
            var vnext = Program.Discord.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                Console.WriteLine("VNext is not enabled or configured.");
                return;
            }

            // check whether we aren't already connected
            var vnc = vnext.GetConnection(chn.Guild);

            // wait for current playback to finish
            while (vnc.IsPlaying)
                await vnc.WaitForPlaybackFinishAsync();


            // play
            Exception exc = null;
            Console.WriteLine($"Playing `text`");
            await vnc.SendSpeakingAsync(true);
            try
            {
                // borrowed from
                // https://github.com/RogueException/Discord.Net/blob/5ade1e387bb8ea808a9d858328e2d3db23fe0663/docs/guides/voice/samples/audio_create_ffmpeg.cs

                var ffmpeg_inf = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"speak.wav\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                var ffmpeg = Process.Start(ffmpeg_inf);
                var ffout = ffmpeg.StandardOutput.BaseStream;

                var txStream = vnc.GetTransmitStream();
                await ffout.CopyToAsync(txStream);
                await txStream.FlushAsync();
                await vnc.WaitForPlaybackFinishAsync(); // wait until playback finishes
            }
            catch (Exception ex) { exc = ex; }
            finally
            {
                await vnc.SendSpeakingAsync(false);
            }

            if (exc != null)
                Console.WriteLine($"An exception occured during playback: `{exc.GetType()}: {exc.Message}`");
        }

        private static async Task Join(DiscordChannel chn)
        {
           await Task.Run(async () =>
           {
                // check whether VNext is enabled
                var vnext = Program.Discord.GetVoiceNext();
               if (vnext == null)
               {
                    // not enabled
                    Console.WriteLine("VNext is not enabled or configured.");
                   return;
               }

                // check whether we aren't already connected
                var vnc = vnext.GetConnection(chn.Guild);
               if (vnc != null)
               {
                    // already connected
                    Console.WriteLine("Already connected in this guild.");
                   return;
               }

               // connect
               Console.WriteLine($"Connecting to `{chn.Name}`.......");
               vnc = await vnext.ConnectAsync(chn);
               Console.WriteLine($"Connected to `{chn.Name}`");
           });
            return;
        }
        private static void Leave(DiscordChannel chn)
        {
            // check whether VNext is enabled
            var vnext = Program.Discord.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                Console.WriteLine("VNext is not enabled or configured.");
                return;
            }

            // check whether we are connected
            var vnc = vnext.GetConnection(chn.Guild);

            // disconnect
            vnc.Disconnect();
            Console.WriteLine("Disconnected");
        }
        private static async Task Play(DiscordChannel chn,string filename)
        {
            // check whether VNext is enabled
            var vnext = Program.Discord.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                Console.WriteLine("VNext is not enabled or configured.");
                return;
            }

            // check whether we aren't already connected
            var vnc = vnext.GetConnection(chn.Guild);
            // check if file exists
            if (!File.Exists(filename))
            {
                // file does not exist
                Console.WriteLine($"File `{filename}` does not exist.");
                return;
            }

            // wait for current playback to finish
            while (vnc.IsPlaying)
                await vnc.WaitForPlaybackFinishAsync();
            

            // play
            Exception exc = null;
            Console.WriteLine($"Playing `{filename}`");
            await vnc.SendSpeakingAsync(true);
            try
            {
                // borrowed from
                // https://github.com/RogueException/Discord.Net/blob/5ade1e387bb8ea808a9d858328e2d3db23fe0663/docs/guides/voice/samples/audio_create_ffmpeg.cs

                var ffmpeg_inf = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{filename}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                var ffmpeg = Process.Start(ffmpeg_inf);
                var ffout = ffmpeg.StandardOutput.BaseStream;

                var txStream = vnc.GetTransmitStream();
                await ffout.CopyToAsync(txStream);
                await txStream.FlushAsync();
                await vnc.WaitForPlaybackFinishAsync(); // wait until playback finishes
            }
            catch (Exception ex) { exc = ex; }
            finally
            {
                await vnc.SendSpeakingAsync(false);
            }

            if (exc != null)
                Console.WriteLine($"An exception occured during playback: `{exc.GetType()}: {exc.Message}`");
        }

        public static async Task Speak(DiscordChannel chn,string message)
        {
            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    await PrepareText(message);
                    await Join(chn);
                    await SpeakText(chn);
                    Leave(chn);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    await Program.SendFatalError(Program.errorChannel, "КРИТИЧЕСКАЯ ОШИБКА", $"{ex.Message}\n\n``` {ex.StackTrace} ```");
                }
            });
            return;
        }
    }
}
