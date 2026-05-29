; ============================================================
; RADICA 6510 UNITY DEMO
; Author: Fabrizio Radica
; Version: 1.0
; Description: Full Mini6510 demo -- scrolltext, copper bars,
;              sprites, joystick input and sound.
; ============================================================

; Zero Page usage:
; $00 = scroll position (0 to SCROLL_LEN-1)
; $01 = frame tick counter
; $02 = sprite X
; $03 = sprite Y
; $04 = sprite horizontal direction (0=right, 1=left)
; $05 = sound note index (cycles 0-3)
; $06 = joystick temp

        .ORG $1000

START:
        JSR CLEAR_SCREEN

        LDA #$06
        STA $D020
        LDA #$00
        STA $D021

        JSR WRITE_TITLE
        JSR COLOR_TITLE
        JSR INIT_SCROLL
        JSR INIT_COPPER
        JSR INIT_SPRITE
        JSR INIT_SOUND

        LDA #$00
        STA $00
        STA $01
        STA $04
        STA $05
        LDA #$64
        STA $02
        LDA #$50
        STA $03

MAIN_LOOP:
WAIT_VSYNC:
        LDA $FF01
        BEQ WAIT_VSYNC

        INC $01

        JSR UPDATE_SCROLL
        JSR UPDATE_COPPER
        JSR UPDATE_SPRITE
        JSR UPDATE_SOUND
        JSR READ_JOYSTICK

        JMP MAIN_LOOP

; ============================================================
CLEAR_SCREEN:
        LDA #$20
        LDX #$00
CS_LOOP:
        STA $0400,X
        STA $0500,X
        STA $0600,X
        INX
        BNE CS_LOOP
        LDX #$00
CS_LOOP2:
        STA $06E8,X
        INX
        CPX #$18
        BNE CS_LOOP2
        RTS

; ============================================================
WRITE_TITLE:
        LDX #$00
WT_LOOP:
        LDA TITLE_TEXT,X
        BEQ WT_DONE
        STA $0409,X
        INX
        JMP WT_LOOP
WT_DONE:
        RTS

TITLE_TEXT:
        ; "RADICA 6510 UNITY DEMO" in PETSCII
        .BYTE $12,$01,$04,$09,$03,$01,$20,$36,$35,$31,$30,$20
        .BYTE $15,$0E,$09,$14,$19,$20,$04,$05,$0D,$0F
        .BYTE $00

; ============================================================
COLOR_TITLE:
        LDX #$00
CT_LOOP:
        LDA #$07
        STA $D800,X
        LDA #$0E
        STA $D828,X
        INX
        CPX #$28
        BNE CT_LOOP
        RTS

; ============================================================
INIT_SCROLL:
        LDX #$00
IS_LOOP:
        LDA SCROLL_TEXT,X
        STA $0BE8,X
        INX
        CPX #$28
        BNE IS_LOOP
        RTS

SCROLL_TEXT:
        .BYTE $20,$20,$20,$20,$20,$2A,$2A,$2A,$20
        .BYTE $12,$01,$04,$09,$03,$01,$20
        .BYTE $36,$35,$31,$30,$20
        .BYTE $15,$0E,$09,$14,$19,$20
        .BYTE $04,$05,$0D,$0F,$20,$2A,$2A,$2A
        .BYTE $20,$20,$20,$20,$20,$20,$20

UPDATE_SCROLL:
        LDA $01
        AND #$03
        BNE US_DONE

        LDX #$00
US_SHIFT:
        LDA $07A9,X
        STA $07A8,X
        INX
        CPX #$27
        BNE US_SHIFT

        LDA $00
        TAX
        LDA SCROLL_TEXT,X
        STA $07CF

        INC $00
        LDA $00
        CMP #$28
        BNE US_DONE
        LDA #$00
        STA $00
US_DONE:
        RTS

; ============================================================
INIT_COPPER:
        LDA #$14
        STA $D100
        LDA #$0C
        STA $D101
        LDA #$03
        STA $D102
        LDA #$01
        STA $D103
        LDA #$01
        STA $D104

        LDA #$3C
        STA $D108
        LDA #$0C
        STA $D109
        LDA #$05
        STA $D10A
        LDA #$FF
        STA $D10B
        LDA #$01
        STA $D10C

        LDA #$64
        STA $D110
        LDA #$0C
        STA $D111
        LDA #$04
        STA $D112
        LDA #$02
        STA $D113
        LDA #$01
        STA $D114

        LDA #$8C
        STA $D118
        LDA #$0C
        STA $D119
        LDA #$08
        STA $D11A
        LDA #$FE
        STA $D11B
        LDA #$01
        STA $D11C
        RTS

UPDATE_COPPER:
        LDA $01
        AND #$07
        BNE UC_DONE
        LDA $D102
        CLC
        ADC #$01
        AND #$0F
        STA $D102
UC_DONE:
        RTS

; ============================================================
INIT_SPRITE:
        LDA #$80
        STA $07F8

        LDX #$00
        LDA #$00
ISP_BLANK:
        STA $2000,X
        INX
        CPX #$3F
        BNE ISP_BLANK

        ; Rows 10-11 center
        LDA #$FF
        STA $2019
        STA $201A
        STA $201B
        ; Rows 8-9
        LDA #$3F
        STA $2015
        LDA #$FC
        STA $2017
        ; Rows 12-13
        LDA #$3F
        STA $2021
        LDA #$FC
        STA $2023
        ; Rows 6-7
        LDA #$0F
        STA $200F
        LDA #$F0
        STA $2011
        ; Rows 14-15
        LDA #$0F
        STA $2027
        LDA #$F0
        STA $2029

        LDA #$01
        STA $D015
        LDA #$0D
        STA $D027

        LDA $02
        STA $D000
        LDA $03
        STA $D001
        RTS

UPDATE_SPRITE:
        LDA $04
        BNE USP_LEFT

        INC $02
        LDA $02
        CMP #$C8
        BNE USP_SET_X
        LDA #$01
        STA $04
        JMP USP_SET_X

USP_LEFT:
        DEC $02
        LDA $02
        CMP #$14
        BNE USP_SET_X
        LDA #$00
        STA $04

USP_SET_X:
        LDA $02
        STA $D000
        LDA $03
        STA $D001
        RTS

; ============================================================
INIT_SOUND:
        LDA #$0C
        STA $D418

        LDA #$66
        STA $D400
        LDA #$11
        STA $D401
        LDA #$00
        STA $D402
        LDA #$08
        STA $D403
        LDA #$21
        STA $D405
        LDA #$A2
        STA $D406
        LDA #$41
        STA $D404
        RTS

UPDATE_SOUND:
        LDA $01
        AND #$3F
        BNE UDS_DONE

        LDA $05
        TAX
        LDA NOTE_LO,X
        STA $D400
        LDA NOTE_HI,X
        STA $D401

        LDA #$40
        STA $D404
        LDA #$41
        STA $D404

        INC $05
        LDA $05
        CMP #$04
        BNE UDS_DONE
        LDA #$00
        STA $05
UDS_DONE:
        RTS

NOTE_LO:
        .BYTE $66,$7A,$8F,$AA
NOTE_HI:
        .BYTE $11,$13,$15,$18

; ============================================================
READ_JOYSTICK:
        LDA $DC00
        STA $06

        AND #$01
        BNE RJ_CHECK_DOWN
        DEC $03
        LDA $03
        CMP #$0A
        BCS RJ_CHECK_DOWN
        LDA #$0A
        STA $03

RJ_CHECK_DOWN:
        LDA $06
        AND #$02
        BNE RJ_CHECK_LEFT
        INC $03
        LDA $03
        CMP #$C8
        BCC RJ_CHECK_LEFT
        LDA #$C8
        STA $03

RJ_CHECK_LEFT:
        LDA $06
        AND #$04
        BNE RJ_CHECK_RIGHT
        DEC $02
        LDA $02
        CMP #$0A
        BCS RJ_CHECK_RIGHT
        LDA #$0A
        STA $02

RJ_CHECK_RIGHT:
        LDA $06
        AND #$08
        BNE RJ_DONE
        INC $02
        LDA $02
        CMP #$E8
        BCC RJ_DONE
        LDA #$E8
        STA $02

RJ_DONE:
        LDA $02
        STA $D000
        LDA $03
        STA $D001
        RTS
