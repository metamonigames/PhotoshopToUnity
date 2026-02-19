#!/bin/bash
SESSION_NAME=$(basename "$PWD" | tr ' ' '_')
export CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1

tmux kill-session -t "$SESSION_NAME" 2>/dev/null
tmux new-session -d -s "$SESSION_NAME" -n "lead"
tmux set-option -g mouse on

tmux send-keys -t "$SESSION_NAME:lead" \
  "export CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1 && claude --dangerously-skip-permissions --teammate-mode tmux" Enter

tmux set-option -t "$SESSION_NAME" pane-border-status top
tmux set-option -t "$SESSION_NAME" pane-border-format " #{pane_index}: #{pane_title} "
tmux select-pane -t "$SESSION_NAME:lead.0" -T "Lead"

TMUX= tmux attach -t "$SESSION_NAME"