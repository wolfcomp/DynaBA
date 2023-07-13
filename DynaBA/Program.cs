﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Runtime.InteropServices;
using Discord.Net;
using DynaBA.Enums;
using DynaBA.Models;
using Newtonsoft.Json;

namespace DynaBA;

internal class Program
{
    public static DiscordSocketClient Client;
    public IServiceProvider Services;

    private bool _run = true;

    public static Task Main(string[] args) => new Program().MainAsync();

    private async Task MainAsync()
    {
        var token = Yaml.Parse<Bot>("token.yml");

        Services = ConfigureServices();

        Client = Services.GetRequiredService<DiscordSocketClient>();

        Client.Log += ClientOnLog;

        Client.Ready += ClientOnReady;
        Client.SlashCommandExecuted += ClientOnSlashCommandExecuted;

        await Client.LoginAsync(TokenType.Bot, token.Token);
        await Client.StartAsync();

        Console.WriteLine("Ready! Started service.");

        PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ => { _run = false; });

        while (_run)
        {
            await Task.Delay(1000);
        }

        await Client.LogoutAsync();
    }

    private Task ClientOnLog(LogMessage arg)
    {
        Console.WriteLine(arg.ToString());
        return Task.CompletedTask;
    }

    private async Task ClientOnSlashCommandExecuted(SocketSlashCommand arg)
    {
        switch (arg.CommandName)
        {
            case "eureka":
                var data = arg.Data.Options.First();
                await arg.RespondAsync("thonk");

                var yamlData = Yaml.Parse<Dictionary<string, Eureka>>("eureka.yml");

                if (yamlData.TryGetValue(data.Name, out var eurekaCommand))
                {
                    var embed = new EmbedBuilder()
                        .WithTitle(eurekaCommand.Title)
                        .WithDescription(eurekaCommand.Content)
                        .WithImageUrl(eurekaCommand.Image);

                    await arg.ModifyOriginalResponseAsync(msg =>
                    {
                        msg.Content = null;
                        msg.Embed = embed.Build();
                    });
                }
                else
                {
                    var embed = new EmbedBuilder();

                    switch (data.Name)
                    {
                        case "bis":
                            var gearType = (GearType)data.Options.First().Value;

                            embed.WithTitle($"Eurekan Best in Slot for {gearType}");
                            
                            switch (gearType)
                            {
                                case GearType.Fending:
                                    embed.WithDescription("Elemental +2 gear is your Eureka end-game, alongside the Kirin's Osode of Fending.")
                                        .WithUrl("https://etro.gg/gearset/ccdfa90d-a16c-4b9e-a5b7-c6b0d3df6fea");
                                    break;
                                case GearType.Healing:
                                    embed.WithDescription("Elemental +2 gear is your Eureka end-game, alongside the Vermilion Cloak of Healing.")
                                        .WithUrl("https://etro.gg/gearset/666db791-128a-43de-ae8a-5b0a89c0fefd");
                                    break;
                                case GearType.Striking:
                                    embed.WithDescription("Elemental +2 gear is your Eureka end-game, alongside the Kirin's Osode of Scouting.")
                                        .WithUrl("https://etro.gg/gearset/fcf3ce10-8891-4fe1-9db8-2c2c80be2fd5");
                                    break;
                                case GearType.Scouting:
                                    embed.WithDescription("Elemental +2 gear is your Eureka end-game, alongside the Kirin's Osode of Scouting.")
                                        .WithUrl("https://etro.gg/gearset/cb75f756-9825-4688-a977-9a6ee796988b");
                                    break;
                                case GearType.Maiming:
                                    embed.WithDescription("Elemental +2 gear is your Eureka end-game, alongside the Kirin's Osode of Maiming.")
                                        .WithUrl("https://etro.gg/gearset/88c62a36-8489-4b30-80f1-c8b932bdf15d");
                                    break;
                                case GearType.Aiming:
                                    embed.WithDescription("Elemental +2 gear is your Eureka end-game, alongside the Kirin's Osode of Aiming.")
                                        .WithUrl("https://etro.gg/gearset/c5ba63fb-a671-4716-b35e-c834c863f8b0");
                                    break;
                                case GearType.Casting:
                                    embed.WithDescription("Elemental +2 gear is your Eureka end-game, alongside the Vermilion Cloak of Casting.")
                                        .WithUrl("https://etro.gg/gearset/b573db1c-97fa-4c34-9e49-48a6bd59b1ae");
                                    break;
                            }
                            
                            break;
                    }

                    await arg.ModifyOriginalResponseAsync(msg =>
                    {
                        msg.Content = null;
                        msg.Embed = embed.Build();
                    });
                }

                break;
        }
    }

    private async Task ClientOnReady()
    {
        try
        {
            foreach (var dcg in Client.Guilds)
            {
                var commands = await Client.Rest.GetGuildApplicationCommands(dcg.Id);

                foreach (var command in commands)
                {
                    await command.DeleteAsync();
                }
            }
        }
        catch (HttpException e)
        {
            var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
            await ClientOnLog(new LogMessage(LogSeverity.Error, "BA Helper", $"An HTTP Error occurred! \n{json}"));
        }

        await Client.SetActivityAsync(new Game("the Containment Units", ActivityType.Watching));

        var yamlEurekas = Yaml.Parse<Dictionary<string, Eureka>>("eureka.yml");
        var yamlBAs = Yaml.Parse<Dictionary<string, BaldesionArsenal>>("eureka.yml");

        var eurekaCommandBuilder = new SlashCommandBuilder()
            .WithName("eureka")
            .WithDescription("General Eureka related commands");

        var baCommandBuilder = new SlashCommandBuilder()
            .WithName("ba")
            .WithDescription("BA related commands");

        foreach (var (commandName, commandArgs) in yamlEurekas)
        {
            eurekaCommandBuilder = eurekaCommandBuilder.AddOption(new SlashCommandOptionBuilder()
                .WithName(commandName)
                .WithDescription(commandArgs.Description)
                .WithType(ApplicationCommandOptionType.SubCommand));
        }

        foreach (var (commandName, commandArgs) in yamlBAs)
        {
            baCommandBuilder = baCommandBuilder.AddOption(new SlashCommandOptionBuilder()
                .WithName(commandName)
                .WithDescription(commandArgs.Description)
                .WithType(ApplicationCommandOptionType.SubCommand));
        }

        var gearSlashCommandOptionBuilder = new SlashCommandOptionBuilder()
            .WithName("class")
            .WithDescription("Gives a choice with different gear types")
            .WithType(ApplicationCommandOptionType.Integer)
            .WithRequired(true);

        foreach (var gearSet in Enum.GetValues<GearType>())
        {
            gearSlashCommandOptionBuilder = gearSlashCommandOptionBuilder.AddChoice(gearSet.ToString(), (int)gearSet);
        }

        eurekaCommandBuilder.AddOption(new SlashCommandOptionBuilder().WithName("bis")
            .WithDescription("Shows the best in slot gear for Eureka and the Baldesion Arsenal")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption(gearSlashCommandOptionBuilder)
        );


        try
        {
            foreach (var dcg in Client.Guilds)
            {
                await Client.Rest.CreateGuildCommand(eurekaCommandBuilder.Build(), dcg.Id);
            }

            foreach (var dcg in Client.Guilds)
            {
                await Client.Rest.CreateGuildCommand(baCommandBuilder.Build(), dcg.Id);
            }
        }
        catch (HttpException e)
        {
            var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
            await ClientOnLog(new LogMessage(LogSeverity.Error, "BA Helper", $"An HTTP Error occurred! \n{json}"));
        }
    }

    private IServiceProvider ConfigureServices()
    {
        var disConfig = new DiscordSocketConfig { MessageCacheSize = 100 };

        return new ServiceCollection()
            .AddSingleton(new DiscordSocketClient(disConfig))
            .AddSingleton(provider => new InteractionService(provider.GetRequiredService<DiscordSocketClient>()))
            .BuildServiceProvider();
    }
}

public static class Yaml
{
    private static readonly IDeserializer Deserializer =
        new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

    public static T Parse<T>(string ymlFile)
    {
        var ymlString = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, ymlFile));
        return Deserializer.Deserialize<T>(ymlString);
    }
}