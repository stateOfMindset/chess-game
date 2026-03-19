let board = [];
let selectedSquare = -1;
let turn = 'w';
let gameOver = false;
let castlingRights = { K: true, Q: true, k: true, q: true };
let lastMoveIndices = [];
let isHoldingForPlacingArrows = false;
let ArrowSquareIndexesLog = [];
let ArrowQueue = [];
let isDragging = false;
let dragging_Piece_id = 0;
let selectedPiece;
let isServerMoveOver = true;


const pieceImages = {
    'p': 'Bpawn.png', 'n': 'BKnight.png', 'b': 'BBishop.png', 'r': 'BRook.png', 'q': 'BQueen.png', 'k': 'BKing.png',
    'P': 'Wpawn.png', 'N': 'WKnight.png', 'B': 'WBishop.png', 'R': 'WRook.png', 'Q': 'WQueen.png', 'K': 'WKing.png'
};

//#region Console Related functions , variables.

console.stdlog = console.log.bind(console);
console.logs = [];

console.log = function (arguments) {
    if (arguments == null)
        return;
    if (typeof (arguments) == 'object') {
        console.logs.push(Array.from(arguments));
        console.stdlog.apply(console, Array.from(arguments));
    }
    else {
        console.logs.push(arguments);
        console.stdlog.apply(console, Array.from(arguments));
    }
}

console.getLastLog = function () {
    if (console.logs.length == 0) return null;
    return console.logs[console.logs.length - 1];
}

//#endregion

const initialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

function initBoard(fen) {
    board = new Array(64).fill(null);
    let fenParts = fen.split(' ');
    let rows = fenParts[0].split('/');
    let idx = 0;
    for (let r = 0; r < 8; r++) {
        for (let c = 0; c < rows[r].length; c++) {
            let char = rows[r][c];
            if (!isNaN(char)) idx += parseInt(char);
            else { board[idx] = char; idx++; }
        }
    }
    renderBoard();   
}

function canvasIntializer() {
    const wrapper = document.getElementById('board');
    const canvas = document.createElement('canvas');
    canvas.id = 'myCanvas';

    canvas.style.position = 'fixed';
    canvas.style.pointerEvents = 'none';

    canvas.width = wrapper.offsetWidth;
    canvas.height = wrapper.offsetHeight;

    wrapper.appendChild(canvas);
}
function updateEvalBar(score) {
    let displayScore = (score / 100).toFixed(1);
    let percent = 50 + (score / 10);
    if (percent > 95) percent = 95;
    if (percent < 5) percent = 5;

    document.getElementById('eval-bar').style.height = percent + "%";
    document.getElementById('eval-score').innerText = (score > 0 ? "+" : "") + displayScore;
}

function renderBoard() {
    const boardEl = document.getElementById('board');
    boardEl.innerHTML = '';
    let kingInCheckIdx = -1;
    if (isKingInCheck(turn)) {
        kingInCheckIdx = board.findIndex(p => p === (turn === 'w' ? 'K' : 'k'));
    }
    for (let i = 0; i < 64; i++) {
        let sq = document.createElement('div');
        let row = Math.floor(i / 8);
        let col = i % 8;
        sq.className = 'square ' + ((row + col) % 2 === 0 ? 'white' : 'black');
       // sq.onclick = () => onSquareClick(i);
        sq.addEventListener('mousedown', (event) => onSquareOnMouseDown(i, event));
        sq.addEventListener('mouseup', (event) => onSquareOnMouseUp(i, event));
        sq.addEventListener('mousemove', (event) => DragChessPiece(event));
        sq.addEventListener('contextmenu', (event) => removeContextMenuOnMouseDragForArrows(event));
        sq.id = "tile_" + i.toString();
      

        if (lastMoveIndices.includes(i)) sq.classList.add('last-move');
        if (i === selectedSquare) sq.classList.add('selected');
        if (i === kingInCheckIdx) sq.classList.add('check');

        if (board[i]) {
            let img = document.createElement('img');
            img.setAttribute('draggable',false);
            img.src = 'images/' + pieceImages[board[i]];
            sq.appendChild(img);
        }
        boardEl.appendChild(sq);    
    }
    canvasIntializer();
}

//#region Arrow-related functions.
function DragChessPiece(e) {
    if (!isDragging || dragging_Piece_id == 0)
        return;

    e.preventDefault();
    drag_helper_attributes(e);

}

function findSquareById(idx) {
    return clickedSquare = document.getElementById("tile_"+idx);
}

function onSquareOnMouseDown(idx, event) {
    if (event.button == 2) // if right click 
    { 
        isHoldingForPlacingArrows = true;
        console.log(idx + " has been chosen for arrow starting point")
        ArrowSquareIndexesLog.push(idx);
        
    }

    if (event.button == 0) {
        dragging_Piece_id = idx;
        selectedPiece = findSquareById(dragging_Piece_id).querySelector('img');
        if (selectedPiece != null && selectedPiece.getAttribute('src').includes('images/B'))
            selectedPiece = null;

        if (selectedPiece == null || !isServerMoveOver)
            return;
        isDragging = true;
        document.body.appendChild(selectedPiece);
        drag_helper_attributes(event);
        onSquareClick(idx);
        if (findSquareById(dragging_Piece_id).querySelector('img'))
            findSquareById(dragging_Piece_id).querySelector('img').remove();
       
        
    }
}

function drag_helper_attributes(e) {
    if (selectedPiece == null)
        return;
    selectedPiece.style.position = 'absolute';
    selectedPiece.style.zIndex = '1000';
    selectedPiece.width = 60;
    selectedPiece.height = 60;
    selectedPiece.style.left = (e.clientX - selectedPiece.width / 2) + 'px';
    selectedPiece.style.top = (e.clientY - selectedPiece.height / 2) + 'px';
    selectedPiece.style.pointerEvents = 'none';

}

function onSquareOnMouseUp(idx, event) {
    if (event.button == 2) { // if right click
        console.log(idx + " has been chosen for arrow ending point")
        ArrowSquareIndexesLog.push(idx);
        const [A, B] = [ArrowSquareIndexesLog[ArrowSquareIndexesLog.length - 1], ArrowSquareIndexesLog[ArrowSquareIndexesLog.length - 2]];
        createArrow(A, B);
    }
    if (event.button == 0) {
        if (selectedPiece == null || !isServerMoveOver)
            return;
        isDragging = false;
        dragging_Piece_id = 0;
        document.body.removeChild(selectedPiece);
        onSquareClick(idx);
        
    }

    event.preventDefault();   
}

function createArrow(A, B) {
    const [X1, Y1] = calculateSquareCenter(A);
    const [X2, Y2] = calculateSquareCenter(B);
    addArrowToQueue(X2, Y2, X1, Y1);
}

function addArrowToQueue(fromX, fromY, toX, toY) {
    ArrowQueue.push({ fromX, fromY, toX, toY });
    drawArrow(fromX,fromY,toX,toY);
}

function removeLastArrow() {
    ArrowQueue.pop();
    renderCanvas();
}

function renderCanvas() {
    let canvas = document.getElementById("myCanvas");
    let ctx = canvas.getContext('2d');

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ArrowQueue.forEach(arrow => {
        drawArrow(arrow.fromX, arrow.fromY, arrow.toX, arrow.toY);
    });
}

function drawArrow(fromX, fromY, toX, toY) {
    if (fromX == toX && fromY == toY)
        return;
    let canvas = document.getElementById("myCanvas");
    let ctx = canvas.getContext('2d');

    const headLength = 25;
    const headWidth = 24; 
    const shaftWidth = 8;  
    const angle = Math.atan2(toY - fromY, toX - fromX);

    ctx.fillStyle = "rgba(255, 140, 0, 0.75)";
    ctx.strokeStyle = "rgba(200, 80, 0, 0.8)"; 
    ctx.lineWidth = 1;

    ctx.beginPath();

    ctx.moveTo(
        fromX + (shaftWidth / 2) * Math.cos(angle - Math.PI / 2),
        fromY + (shaftWidth / 2) * Math.sin(angle - Math.PI / 2)
    );

    ctx.lineTo(
        toX - headLength * Math.cos(angle) + (shaftWidth / 2) * Math.cos(angle - Math.PI / 2),
        toY - headLength * Math.sin(angle) + (shaftWidth / 2) * Math.sin(angle - Math.PI / 2)
    );

    ctx.lineTo(
        toX - headLength * Math.cos(angle) + (headWidth / 2) * Math.cos(angle - Math.PI / 2),
        toY - headLength * Math.sin(angle) + (headWidth / 2) * Math.sin(angle - Math.PI / 2)
    );

    ctx.lineTo(toX, toY);

    ctx.lineTo(
        toX - headLength * Math.cos(angle) + (headWidth / 2) * Math.cos(angle + Math.PI / 2),
        toY - headLength * Math.sin(angle) + (headWidth / 2) * Math.sin(angle + Math.PI / 2)
    );

    ctx.lineTo(
        toX - headLength * Math.cos(angle) + (shaftWidth / 2) * Math.cos(angle + Math.PI / 2),
        toY - headLength * Math.sin(angle) + (shaftWidth / 2) * Math.sin(angle + Math.PI / 2)
    );

    ctx.lineTo(
        fromX + (shaftWidth / 2) * Math.cos(angle + Math.PI / 2),
        fromY + (shaftWidth / 2) * Math.sin(angle + Math.PI / 2)
    );

    ctx.closePath(); 
    ctx.fill();    
    ctx.stroke();   
}

function calculateSquareCenter(idx) {
    const board = document.getElementById('board');
    const tile = document.getElementById('tile_' + idx.toString());
    var rect = tile.getBoundingClientRect();
    const boardCords = board.getBoundingClientRect();
    return [rect.left + 30  - boardCords.left, rect.top + 30 - boardCords.top];
   

}

function removeContextMenuOnMouseDragForArrows(event) {
    if (isHoldingForPlacingArrows) {
        event.preventDefault();
        isHoldingForPlacingArrows = false;
    }
}

//#endregion 
function onSquareClick(idx) {
    if (gameOver) return;
    ArrowQueue = [];
    if (selectedSquare === -1) {
        if (board[idx] && getColor(board[idx]) === turn) {
            selectedSquare = idx;
            renderBoard();
        }
    } else {
        if (board[idx] && getColor(board[idx]) === turn) {
            selectedSquare = idx;
            renderBoard();
        } else {
            if (isLegalMove(selectedSquare, idx)) {
                makeMove(selectedSquare, idx);
                selectedSquare = -1;
                renderBoard();
                checkGameOver();
                if (!gameOver) sendMoveToServer();
            } else {
                selectedSquare = -1;
                renderBoard();
            }
        }
    }
}

function isLegalMove(from, to) {
    let piece = board[from];
    if (piece.toLowerCase() === 'k' && Math.abs(to - from) === 2) {
        if (isKingInCheck(getColor(piece))) return false;
    }
    if (!isPseudoLegal(from, to)) return false;
    let savedTo = board[to], savedFrom = board[from];
    board[to] = board[from]; board[from] = null;
    let kingSafe = !isKingInCheck(getColor(savedFrom));
    board[from] = savedFrom; board[to] = savedTo;
    return kingSafe;
}

function isPseudoLegal(from, to) {
    let piece = board[from]; let type = piece.toLowerCase();
    let r1 = Math.floor(from / 8), c1 = from % 8; let r2 = Math.floor(to / 8), c2 = to % 8;
    let dr = r2 - r1, dc = c2 - c1;
    if (type === 'k') { if (Math.abs(dc) === 2 && dr === 0) return true; return Math.abs(dr) <= 1 && Math.abs(dc) <= 1; }
    let color = getColor(piece);
    if (type === 'p') {
        let dir = (color === 'w') ? -1 : 1; let startRow = (color === 'w') ? 6 : 1;
        if (dc === 0 && dr === dir && !board[to]) return true;
        if (dc === 0 && dr === 2 * dir && r1 === startRow && !board[to] && !board[from + 8 * dir]) return true;
        if (Math.abs(dc) === 1 && dr === dir && board[to]) return true;
        return false;
    }
    if (type === 'n') return (Math.abs(dr) === 2 && Math.abs(dc) === 1) || (Math.abs(dr) === 1 && Math.abs(dc) === 2);
    if (type === 'b' || type === 'r' || type === 'q') {
        if (type === 'r' && dr !== 0 && dc !== 0) return false;
        if (type === 'b' && Math.abs(dr) !== Math.abs(dc)) return false;
        if (type === 'q' && (dr !== 0 && dc !== 0) && (Math.abs(dr) !== Math.abs(dc))) return false;
        let stepR = dr === 0 ? 0 : (dr > 0 ? 1 : -1); let stepC = dc === 0 ? 0 : (dc > 0 ? 1 : -1);
        let curR = r1 + stepR, curC = c1 + stepC;
        while (curR !== r2 || curC !== c2) { if (board[curR * 8 + curC]) return false; curR += stepR; curC += stepC; }
        return true;
    }
    return false;
}

function isKingInCheck(color) { let kingIdx = board.indexOf(color === 'w' ? 'K' : 'k'); if (kingIdx === -1) return true; return isSquareAttacked(kingIdx, color === 'w' ? 'b' : 'w'); }

function isSquareAttacked(sq, byColor) {
    for (let i = 0; i < 64; i++) {
        if (board[i] && getColor(board[i]) === byColor) {
            let p = board[i].toLowerCase();
            if (p === 'k') { let r1 = Math.floor(i / 8), c1 = i % 8, r2 = Math.floor(sq / 8), c2 = sq % 8; if (Math.abs(r1 - r2) <= 1 && Math.abs(c1 - c2) <= 1) return true; }
            else { if (isPseudoLegal(i, sq)) return true; }
        }
    } return false;
}

function hasLegalMoves(color) { for (let f = 0; f < 64; f++) if (board[f] && getColor(board[f]) === color) for (let t = 0; t < 64; t++) if (f !== t && (!board[t] || getColor(board[t]) !== color)) if (isLegalMove(f, t)) return true; return false; }

function checkGameOver() { if (!hasLegalMoves(turn)) { gameOver = true; let s = document.getElementById('status'); if (isKingInCheck(turn)) { s.innerText = "Checkmate!"; s.style.color = "red"; } else { s.innerText = "Stalemate!"; s.style.color = "yellow"; } } }

function getColor(p) { return p === p.toUpperCase() ? 'w' : 'b'; }

function eatPiece(from, to) {
    piece_From = findSquareById(from).querySelector('img');
    piece_to = findSquareById(to).querySelector('img');
    if (piece_From == null || piece_to == null)
        return;

    document.getElementById('status').innerText = (turn === 'w' ? "White" : "Black") + " to move";
    const container = (piece_to.getAttribute('src').includes('images/B') ? "black" : "white");
    const defeated_pieces_containmer = document.getElementById(container +"-defeated-pieces-container");
    defeated_pieces_containmer.appendChild(piece_to);


}

function makeMove(from, to) {
    
    eatPiece(from, to);
    lastMoveIndices = [from, to];

    let piece = board[from];
    if (piece === 'K') { castlingRights.K = false; castlingRights.Q = false; }
    if (piece === 'k') { castlingRights.k = false; castlingRights.q = false; }
    if (piece === 'R' && from === 63) castlingRights.K = false;
    if (piece === 'R' && from === 56) castlingRights.Q = false;
    if (piece === 'r' && from === 7) castlingRights.k = false;
    if (piece === 'r' && from === 0) castlingRights.q = false;

    if (piece.toLowerCase() === 'k' && Math.abs(to - from) === 2) {
        if (to === from + 2) { board[from + 1] = board[from + 3]; board[from + 3] = null; }
        else if (to === from - 2) { board[from - 1] = board[from - 4]; board[from - 4] = null; }
    }

    

    board[to] = board[from]; board[from] = null;
    if (piece === 'P' && Math.floor(to / 8) === 0) board[to] = 'Q';
    if (piece === 'p' && Math.floor(to / 8) === 7) board[to] = 'q';
    turn = turn === 'w' ? 'b' : 'w';
    document.getElementById('status').innerText = (turn === 'w' ? "White" : "Black") + " to move";
    isServerMoveOver = true;
}

function generateFen() {
    let fen = "";
    for (let r = 0; r < 8; r++) {
        let empty = 0;
        for (let c = 0; c < 8; c++) {
            let p = board[r * 8 + c];
            if (!p) empty++; else { if (empty > 0) { fen += empty; empty = 0; } fen += p; }
        }
        if (empty > 0) fen += empty; if (r < 7) fen += "/";
    }
    let cStr = ""; if (castlingRights.K) cStr += "K"; if (castlingRights.Q) cStr += "Q"; if (castlingRights.k) cStr += "k"; if (castlingRights.q) cStr += "q"; if (cStr === "") cStr = "-";
    fen += " " + turn + " " + cStr + " - 0 1";
    return fen;
}

async function sendMoveToServer() {
    if (gameOver) return;
    isServerMoveOver = false;
    document.getElementById('status').innerText = "Engine thinking...";
    let currentFen = generateFen();

    try {
        let response = await fetch('ChessHandler.ashx', {
            method: 'POST',
            body: JSON.stringify({ fen: currentFen }),
            headers: { 'Content-Type': 'application/json' }
        });
        let data = await response.json();

        if (data.move) {
            let parts = data.move.split('|');
            let moveStr = parts[0];
            let score = parseInt(parts[1]);

            if (!isNaN(score)) updateEvalBar(score);

            let fromStr = moveStr.substring(0, 2);
            let toStr = moveStr.substring(2, 4);
            let from = (8 - parseInt(fromStr[1])) * 8 + (fromStr.charCodeAt(0) - 97);
            let to = (8 - parseInt(toStr[1])) * 8 + (toStr.charCodeAt(0) - 97);
            makeMove(from, to);
            renderBoard();
            checkGameOver();
        } else {
            checkGameOver();
        }
    } catch (e) { console.error(e); }
}

initBoard(initialFen);


