// Fijación del formato v1 desde Kotlin (10a 02-tech-design §2.6, §4.5): escribe con Wire.kt y EventRing.kt (los del plugin, sin
// android.*) los 11 registros de Assets/RowMate/Tests/Golden/ble/events-v1.bin; native/android/check.ps1 -Golden lo corre en la
// JVM y compara los bytes. Lo mismo que Tests/Support/BleWireFixture.Encode() (C#) y rm_ble_wire_fixture (DLL de Windows). Solo
// los ids ficticios de K-10. Además prueba el descarte del anillo (los más viejos, y un Dropped primero) y el tope del drenaje.
//   kotlin EmitFixtureKt <salida.bin>
package com.rowmate.ble

import java.io.File
import kotlin.system.exitProcess

fun main(args: Array<String>) {
    if (args.size != 1) {
        System.err.println("uso: EmitFixtureKt <salida.bin>")
        exitProcess(64)
    }

    val row = "AA:BB:CC:00:06:70"
    val anon = "AA:BB:CC:00:00:02"
    val ftms = Wire.shortUuid(0x1826)
    val rowerData = Wire.shortUuid(0x2AD1)
    val controlPoint = Wire.shortUuid(0x2AD9)
    val packet = byteArrayOf(0x2c, 0x00, 0x30, 0x2a, 0x01, 0xe2.toByte(), 0x01, 0x00, 0x85.toByte(), 0x00, 0x96.toByte(), 0x00)

    val ring = EventRing(Wire.RING_BYTES) { 0L }
    ring.push(Wire.adapterState(1.5, Wire.STATE_ON))
    ring.push(Wire.scanResult(2.0, row, "AMS-670B", -61, listOf(ftms)))
    ring.push(Wire.scanResult(2.25, anon, null, -75, listOf(ftms, Wire.shortUuid(0x180D))))
    ring.push(Wire.scanFailed(3.0, Wire.ERROR_PERMISSION, "permission denied"))
    ring.push(Wire.connection(4.0, row, true, 0))
    ring.push(Wire.requestDone(4.5, 1, Wire.OK, null))
    val service = Wire.Service(
        ftms,
        listOf(
            Wire.Characteristic(rowerData, Wire.PROP_NOTIFY),
            Wire.Characteristic(controlPoint, Wire.PROP_WRITE or Wire.PROP_INDICATE)
        )
    )
    ring.push(Wire.discoverDone(5.0, 2, listOf(service)))
    ring.push(Wire.requestDone(5.5, 3, 133, "GATT error 133"))
    ring.push(Wire.notify(6.0, row, rowerData, packet))
    ring.push(Wire.connection(7.0, row, false, 8))
    ring.push(Wire.dropped(8.0, 3))

    val expectedLength = ring.pending()
    val bytes = ring.drain()
    if (bytes.size != expectedLength || ring.pending() != 0) {
        System.err.println("drain: ${bytes.size} bytes, pending ${ring.pending()} (se esperaban $expectedLength y 0)")
        exitProcess(1)
    }
    File(args[0]).writeBytes(bytes)

    // El anillo con tope: 3 registros de 16 bytes en 40 -> descarta el más viejo; el drenaje empieza con Dropped(1) a la hora
    // del reloj inyectado (2 s) y el tope de drenaje deja registros enteros para el siguiente.
    val small = EventRing(40) { 2_000_000_000L }
    small.push(Wire.adapterState(0.1, Wire.STATE_OFF))
    small.push(Wire.adapterState(0.2, Wire.STATE_ON))
    small.push(Wire.adapterState(0.3, Wire.STATE_OFF))
    verify(small.pending() == 16 * 3, "pending con descarte")
    val first = small.drain(32)
    verify(first.size == 32, "drenaje acotado")
    verify(first[0].toInt() == Wire.KIND_DROPPED && first[12].toInt() == 1, "Dropped primero con la cuenta")
    verify(java.lang.Double.longBitsToDouble(readLong(first, 4)) == 2.0, "hora del Dropped")
    verify(java.lang.Double.longBitsToDouble(readLong(first, 16 + 4)) == 0.2, "el más viejo descartado")
    val second = small.drain()
    verify(second.size == 16 && small.pending() == 0, "el resto en el drenaje siguiente")
    verify(java.lang.Double.longBitsToDouble(readLong(second, 4)) == 0.3, "orden")
    println("EmitFixture: ${bytes.size} bytes; anillo: ok")
}

private fun verify(condition: Boolean, what: String) {
    if (!condition) {
        System.err.println("EventRing: falla '$what'")
        exitProcess(1)
    }
}

private fun readLong(bytes: ByteArray, offset: Int): Long {
    var value = 0L
    for (i in 7 downTo 0) {
        value = (value shl 8) or (bytes[offset + i].toLong() and 0xFF)
    }
    return value
}
