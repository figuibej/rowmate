//
//  Generated file. Do not edit.
//

// clang-format off

#import "GeneratedPluginRegistrant.h"

#if __has_include(<flutter_blue_plus_darwin/FlutterBluePlusPlugin.h>)
#import <flutter_blue_plus_darwin/FlutterBluePlusPlugin.h>
#else
@import flutter_blue_plus_darwin;
#endif

#if __has_include(<package_info_plus/FPPPackageInfoPlusPlugin.h>)
#import <package_info_plus/FPPPackageInfoPlusPlugin.h>
#else
@import package_info_plus;
#endif

#if __has_include(<sqflite_darwin/SqflitePlugin.h>)
#import <sqflite_darwin/SqflitePlugin.h>
#else
@import sqflite_darwin;
#endif

#if __has_include(<wakelock_plus/WakelockPlusPlugin.h>)
#import <wakelock_plus/WakelockPlusPlugin.h>
#else
@import wakelock_plus;
#endif

@implementation GeneratedPluginRegistrant

+ (void)registerWithRegistry:(NSObject<FlutterPluginRegistry>*)registry {
  [FlutterBluePlusPlugin registerWithRegistrar:[registry registrarForPlugin:@"FlutterBluePlusPlugin"]];
  [FPPPackageInfoPlusPlugin registerWithRegistrar:[registry registrarForPlugin:@"FPPPackageInfoPlusPlugin"]];
  [SqflitePlugin registerWithRegistrar:[registry registrarForPlugin:@"SqflitePlugin"]];
  [WakelockPlusPlugin registerWithRegistrar:[registry registrarForPlugin:@"WakelockPlusPlugin"]];
}

@end
