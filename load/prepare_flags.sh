#!/bin/bash

source ./settings.sh

INDIR="$HERSCHEL_SOURCE/Raw"
OUTDIR="$HERSCHEL_TEMP/flags"

mkdir -p "$OUTDIR"
rm "$OUTDIR/quality.dat"

tail "$INDIR/quality/quality_pacs.txt"           -n+5 | awk '{print "1", $1, $2=="FAILED"?1:0}' >> "$OUTDIR/quality.dat"
tail "$INDIR/quality/quality_parallel_pacs.txt"  -n+5 | awk '{print "1", $1, $2=="FAILED"?1:0}' >> "$OUTDIR/quality.dat"
tail "$INDIR/quality/quality_spire.txt"          -n+5 | awk '{print "2", $1, $2=="FAILED"?1:0}' >> "$OUTDIR/quality.dat"
tail "$INDIR/quality/quality_parallel_spire.txt" -n+5 | awk '{print "2", $1, $2=="FAILED"?1:0}' >> "$OUTDIR/quality.dat"
tail "$INDIR/quality/quality_parallel_pacs.txt"  -n+5 | awk '{print "4", $1, $2=="FAILED"?1:0}' >> "$OUTDIR/quality.dat"
tail "$INDIR/quality/quality_parallel_spire.txt" -n+5 | awk '{print "4", $1, $2=="FAILED"?1:0}' >> "$OUTDIR/quality.dat"
tail "$INDIR/quality/quality_hifi.txt"           -n+5 | awk '{print "8", $1, $2=="FAILED"?1:0}' >> "$OUTDIR/quality.dat"

tail "$INDIR/quality/sso_pacs.txt" -n+5 | awk '{print "1", $1, "1"}' > "$OUTDIR/sso.dat"

rm "$OUTDIR/proposer.dat"

tail "$INDIR/quality/proposer_pacs.txt" -n+2 | awk '{print "1", $1, $2}' > "$OUTDIR/proposer.dat"
tail "$INDIR/quality/proposer_spire.txt" -n+2 | awk '{print "2", $1, $2}' >> "$OUTDIR/proposer.dat"
tail "$INDIR/quality/proposer_parallel.txt" -n+2 | sort | uniq | awk '{print "4", $1, $2}' >> "$OUTDIR/proposer.dat"
tail "$INDIR/quality/proposer_parallel.txt" -n+2 | sort | uniq | awk '{print "1", $1, $2}' >> "$OUTDIR/proposer.dat"
tail "$INDIR/quality/proposer_parallel.txt" -n+2 | sort | uniq | awk '{print "2", $1, $2}' >> "$OUTDIR/proposer.dat"
tail "$INDIR/quality/proposer_hifi.txt" -n+2 | awk '{print "8", $1, $2}' >> "$OUTDIR/proposer.dat"