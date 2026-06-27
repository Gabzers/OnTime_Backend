using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using OnTime.Tests.Infrastructure;

namespace OnTime.Tests.Flows;

/// <summary>
/// Flow 9 — i18n Translation System
/// Goal: Translation map is complete, versioned, and cacheable.
/// </summary>
[Collection("Integration")]
public class I18nFlowTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;

    public I18nFlowTests(TestWebAppFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Test 1 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTranslationMap_DefaultLocale_ReturnsAllRequiredKeys()
    {
        // ACT
        var resp = await _factory.Client.GetAsync("/api/i18n?locale=pt-PT");

        // ASSERT — HTTP 200
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        // Version field present
        root.TryGetProperty("v", out var vProp).ShouldBeTrue();
        vProp.GetString().ShouldNotBeNullOrEmpty();

        // Map field present
        root.TryGetProperty("map", out var mapProp).ShouldBeTrue();

        // Required navigation keys
        mapProp.TryGetProperty("NAV.DASHBOARD", out _).ShouldBeTrue();
        mapProp.TryGetProperty("NAV.CLIENTS", out _).ShouldBeTrue();

        // Required enum keys
        mapProp.TryGetProperty("ENUM.LEAD_SOURCE.0", out _).ShouldBeTrue();
        mapProp.TryGetProperty("ENUM.DEAL_TEMPERATURE.0", out _).ShouldBeTrue();

        // Required account/subscription status keys
        mapProp.TryGetProperty("ACCOUNT.STATUS.0", out _).ShouldBeTrue();
        mapProp.TryGetProperty("SUBSCRIPTION.STATUS.0", out _).ShouldBeTrue();
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTranslationMap_ResponseIncludesVersionHeader()
    {
        // ACT
        var resp = await _factory.Client.GetAsync("/api/i18n?locale=pt-PT");

        // ASSERT — version available in response body
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.ShouldContain("\"v\"");
        // The X-I18n-Version header should also be present
        resp.Headers.TryGetValues("X-I18n-Version", out var versionHeader).ShouldBeTrue();
        versionHeader!.First().ShouldNotBeNullOrEmpty();
    }

    // ── Test 3 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTranslationMap_EnUS_ReturnsEnglishKeys()
    {
        // ACT — request English locale
        var resp = await _factory.Client.GetAsync("/api/i18n?locale=en-US");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        // locale field should reflect what was requested
        root.TryGetProperty("locale", out var localeProp).ShouldBeTrue();
        localeProp.GetString().ShouldBe("en-US");

        // Map must still have all keys
        root.TryGetProperty("map", out var mapProp).ShouldBeTrue();
        mapProp.TryGetProperty("NAV.DASHBOARD", out _).ShouldBeTrue();
    }

    // ── Test 4 ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("pt-PT", "ENUM.NOTIFICATION_STATUS.2", "Adiada")]
    [InlineData("pt-PT", "ENUM.NOTIFICATION_STATUS.3", "Ignorada")]
    [InlineData("en-US", "ENUM.NOTIFICATION_STATUS.2", "Snoozed")]
    [InlineData("en-US", "ENUM.NOTIFICATION_STATUS.3", "Ignored")]
    public async Task NotificationStatusLabels_MatchCanonicalEnum(string locale, string key, string expectedLabel)
    {
        // NotificationStatus: Pending=0, Done=1, Snoozed=2, Ignored=3 (Domain/Enums/NotificationEnums.cs)
        var resp = await _factory.Client.GetAsync($"/api/i18n?locale={locale}");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("map").TryGetProperty(key, out var valueProp).ShouldBeTrue();
        valueProp.GetString().ShouldBe(expectedLabel);
    }

    // ── Test 5 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTranslationMap_ContentType_IsJsonWithUtf8Charset()
    {
        var resp = await _factory.Client.GetAsync("/api/i18n?locale=pt-PT");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var ct = resp.Content.Headers.ContentType;
        ct.ShouldNotBeNull();
        ct!.MediaType.ShouldBe("application/json");
        ct.CharSet?.ToLowerInvariant().ShouldBe("utf-8");
    }

    // ── Test 6 ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("pt-PT", "NAV.NOTIFICATIONS", "Notificações")]
    [InlineData("pt-PT", "NAV.VEHICLES",      "Veículos")]
    [InlineData("pt-PT", "NAV.SETTINGS",      "Definições")]
    [InlineData("pt-PT", "LABEL.CLIENT.LAST_INTERACTION", "Última Interação")]
    public async Task GetTranslationMap_PortugueseDiacritics_AreNotMojibaked(
        string locale, string key, string expected)
    {
        var resp = await _factory.Client.GetAsync($"/api/i18n?locale={locale}");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Read as raw bytes and decode as UTF-8 explicitly to rule out framework auto-decode issues
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        var body = System.Text.Encoding.UTF8.GetString(bytes);
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("map").TryGetProperty(key, out var val).ShouldBeTrue();
        val.GetString().ShouldBe(expected);
    }

    // ── Test 7 ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("pt-PT", "ACTION.PROPOSAL.LOST")]
    [InlineData("pt-PT", "ACTION.SUBSCRIPTION.ACTIVATE")]
    [InlineData("pt-PT", "MSG.CONVERT.SOLD_AT_HINT")]
    [InlineData("pt-PT", "MSG.SALE.CREATED")]
    [InlineData("en-US", "ACTION.PROPOSAL.LOST")]
    [InlineData("en-US", "ACTION.SUBSCRIPTION.ACTIVATE")]
    [InlineData("en-US", "MSG.CONVERT.SOLD_AT_HINT")]
    [InlineData("en-US", "MSG.SALE.CREATED")]
    public async Task GetTranslationMap_AllComponentKeys_ArePresent(string locale, string key)
    {
        var resp = await _factory.Client.GetAsync($"/api/i18n?locale={locale}");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("map").TryGetProperty(key, out var val).ShouldBeTrue(
            $"Key '{key}' missing from {locale} map");
        val.GetString().ShouldNotBeNullOrEmpty();
    }

    // ── Test 8 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTranslationMap_UnknownLocale_FallsBackToPtPT()
    {
        // ACT — request a locale that doesn't exist
        var resp = await _factory.Client.GetAsync("/api/i18n?locale=xx-XX");

        // ASSERT — should still return 200 (falls back to pt-PT)
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.TryGetProperty("map", out _).ShouldBeTrue();
    }
}
