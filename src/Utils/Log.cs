using System;
using Vintagestory.API.Common;

namespace VSInstrumentsBase.src.Utils;

public static class Log
{
  // config flags. server defaults to off to avoid spam, client defaults to on
  public static bool EnableServerDebug { get; set; } = false;
  public static bool EnableClientDebug { get; set; } = true;

  private static bool ShouldDebug(ICoreAPI api)
  {
    if (api == null) return false;
    return api.Side == EnumAppSide.Server ? EnableServerDebug : EnableClientDebug;
  }

  public static void Debug(ICoreAPI api, string tag, string message)
  {
    if (!ShouldDebug(api)) return;
    api.Logger.Debug($"[VSInstruments:{tag}] {message}");
  }

  public static void Debug(ICoreAPI api, string tag, string format, params object[] args)
  {
    if (!ShouldDebug(api)) return;
    api.Logger.Debug($"[VSInstruments:{tag}] {format}", args);
  }

  public static void Notification(ICoreAPI api, string tag, string message)
  {
    api.Logger.Notification($"[VSInstruments:{tag}] {message}");
  }

  public static void Notification(ICoreAPI api, string tag, string format, params object[] args)
  {
    api.Logger.Notification($"[VSInstruments:{tag}] {format}", args);
  }

  public static void Warning(ICoreAPI api, string tag, string message)
  {
    api.Logger.Warning($"[VSInstruments:{tag}] {message}");
  }

  public static void Warning(ICoreAPI api, string tag, string format, params object[] args)
  {
    api.Logger.Warning($"[VSInstruments:{tag}] {format}", args);
  }

  public static void Warning(ICoreAPI api, string tag, string message, Exception exception)
  {
    api.Logger.Warning($"[VSInstruments:{tag}] {message}: {exception.Message}");
    api.Logger.Warning(exception);
  }

  public static void Error(ICoreAPI api, string tag, string message)
  {
    api.Logger.Error($"[VSInstruments:{tag}] {message}");
  }

  public static void Error(ICoreAPI api, string tag, string format, params object[] args)
  {
    api.Logger.Error($"[VSInstruments:{tag}] {format}", args);
  }

  public static void Error(ICoreAPI api, string tag, string message, Exception exception)
  {
    api.Logger.Error($"[VSInstruments:{tag}] {message}: {exception.Message}");
    api.Logger.Error(exception);
  }

  public static void VerboseDebug(ICoreAPI api, string tag, string message)
  {
    if (!ShouldDebug(api)) return;
    api.Logger.VerboseDebug($"[VSInstruments:{tag}] {message}");
  }

  public static void VerboseDebug(ICoreAPI api, string tag, string format, params object[] args)
  {
    if (!ShouldDebug(api)) return;
    api.Logger.VerboseDebug($"[VSInstruments:{tag}] {format}", args);
  }
}
