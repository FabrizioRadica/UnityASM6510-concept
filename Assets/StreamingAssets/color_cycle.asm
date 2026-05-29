; ============================================================
; COLOR CYCLE DEMO
; Author: Fabrizio Radica
; Version: 1.0
; Description: Cycles border and background colors through the
;              full C64 palette while displaying "RADICA DEMO"
;              centered on screen with cycling text color.
; ============================================================

; Zero Page:
; $00 = frame counter
; $01 = border color index   (0-15, cycles every 8 frames)
; $02 = bg color index       (0-15, cycles every 16 frames, offset by 8)
; $03 = text color index     (0-15, cycles every 4 frames)

        .ORG $1000

START:
        JSR CLEAR_SCREEN

        ; Initial colors
        LDA #$06            ; Blue border
        STA $D020
        STA $01
        LDA #$00            ; Black background
        STA $D021
        STA $02
        LDA #$07            ; Yellow text
        STA $03

        ; Zero frame counter
        LDA #$00
        STA $00

        JSR WRITE_TEXT
        JSR COLOR_TEXT

MAIN_LOOP:
WAIT_VS:
        LDA $FF01
        BEQ WAIT_VS

        INC $00

        ; ---- Border color: cycle every 8 frames ----
        LDA $00
        AND #$07
        BNE SKIP_BORDER
        INC $01
        LDA $01
        AND #$0F
        STA $01
        STA $D020
SKIP_BORDER:

        ; ---- Background color: cycle every 16 frames, offset ----
        LDA $00
        AND #$0F
        BNE SKIP_BG
        INC $02
        LDA $02
        AND #$0F
        STA $02
        STA $D021
SKIP_BG:

        ; ---- Text color: cycle every 4 frames ----
        LDA $00
        AND #$03
        BNE SKIP_TEXT_COLOR
        INC $03
        LDA $03
        AND #$0F
        STA $03
        JSR COLOR_TEXT
SKIP_TEXT_COLOR:

        JMP MAIN_LOOP

; ============================================================
; CLEAR_SCREEN: fill Video RAM rows 0-24 with spaces
; ============================================================
CLEAR_SCREEN:
        LDA #$20
        LDX #$00
CSL1:
        STA $0400,X
        STA $0500,X
        STA $0600,X
        INX
        BNE CSL1
        LDX #$00
CSL2:
        STA $06E8,X
        INX
        CPX #$18
        BNE CSL2
        RTS

; ============================================================
; WRITE_TEXT: print "RADICA DEMO" centered at row 12, col 14
; Video RAM address: $0400 + 12*40 + 14 = $05EE
; ============================================================
WRITE_TEXT:
        LDX #$00
WT_LOOP:
        LDA TEXT,X
        BEQ WT_DONE
        STA $05EE,X
        INX
        JMP WT_LOOP
WT_DONE:
        RTS

TEXT:
        ; "RADICA DEMO" in Mini6510 PETSCII
        ; R=$12 A=$01 D=$04 I=$09 C=$03 A=$01 sp=$20 D=$04 E=$05 M=$0D O=$0F
        .BYTE $12,$01,$04,$09,$03,$01,$20,$04,$05,$0D,$0F
        .BYTE $00

; ============================================================
; COLOR_TEXT: write current text color ($03) to Color RAM
;             for all 11 chars at row 12, col 14
; Color RAM address: $D800 + 12*40 + 14 = $D9EE
; ============================================================
COLOR_TEXT:
        LDA $03
        LDX #$00
CT_LOOP:
        STA $D9EE,X
        INX
        CPX #$0B            ; 11 characters
        BNE CT_LOOP
        RTS
