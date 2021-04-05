toastr.options = {
  'timeOut': 2000,
  'showDuration': 200,
  'hideDuration': 200,
  'preventDuplicates': true,
  'positionClass': 'toast-top-center',
};
const roomStatus = ['等待中', '進行中'];

let app = new Vue({
  el: '#app',
  data: {
    scenes: 'initial',
    userName: '',
    color: '',
    connection: new signalR.HubConnectionBuilder().withUrl("/goBangHub").build(),
    pieceArray: (function() {
      let arr = new Array(16);
      for (let i = 0; i <= 15; i++) {
        arr[i] = new Array(16);
      }
      return arr;
    })(),
    userCount: null,
    roomList: [],
    newRoomName: '',
    roomId: '',
    playerList: [],
    isReady: false,
    isPlaying: false,
    isGuest: null,
    timeLeft: null,
    turnIndex: null,
    readyArray: [false, false],
    message: '',
    messageQueue: [],
  },
  methods: {
    joinHall() {
      if (this.userName === '') {
        toastr.error('請輸入ID');
        return;
      }
      this.connection.invoke('CreateUser', app.userName);
      this.scenes = 'hall';
    },
    createRoom(e) {
      if (this.newRoomName === '') {
        toastr.error('請輸入房間名稱');
        e.preventDefault();
        return;
      }
      this.connection.invoke('CreateRoom', app.newRoomName);
      this.scenes = 'room';
      this.$nextTick(() => {
        this.setBoard();
      });
    },
    joinRoom(roomId) {
      this.connection.invoke('JoinRoom', roomId);
      this.scenes = 'room';
      this.roomId = roomId;
      this.$nextTick(() => {
        this.setBoard();
        this.resumeLeaveBtn();
      });
    },
    leaveRoom() {
      this.connection.invoke('LeaveRoom', this.roomId);
      this.scenes = 'hall';
      this.roomId = '';
      this.isReady = false;
      this.isPlaying = false;
      this.isGuest = null;
      this.timeLeft = null;
      this.turnIndex = null;
      this.playerList = [];
      this.message = '';
      this.messageQueue = [];
    },
    setBoard() {
      //綁定XY座標
      let arr = document.querySelectorAll('#piece-group .piece');
      for (let y = 1; y <= 15; y++) {
        for (let x = 1; x <= 15; x++) {
          this.pieceArray[y][x] = arr[(y - 1) * 15 + x - 1];
          this.pieceArray[y][x].x = x;
          this.pieceArray[y][x].y = y;
          this.pieceArray[y][x].onclick = this.setPiece;
        }
      }
      //更新雙方棋子
      this.connection.on('UpdateBoard', function(x, y, color) {
        let piece = app.pieceArray[y][x];
        piece.classList.add('active');
        if (app.color) piece.classList.remove(app.color);
        piece.classList.add(color);
        piece.onclick = null;
      });
    },
    piecePosition(i) {
      return {
        top: `${(Math.floor((i - 1) / 15) + 1) * 40}px`,
        left: `${(i % 15 == 0 ? 15 : i % 15) * 40}px`
      }
    },
    setPiece(e) {
      this.connection.invoke('SetPiece', this.roomId, e.target.x, e.target.y, this.color)
      .catch(function(err) {
        return console.error(err.toString());
      });
    },
    ready() {
      this.connection.invoke('Ready', this.roomId);
      this.isReady = true;
    },
    unready() {
      this.connection.invoke('Unready', this.roomId);
      this.isReady = false;
    },
    sendMessage() {
      if (this.message === '') return;
      this.connection.invoke('SendMessage', this.roomId, this.message);
      this.message = '';
    },
    focusInput() {
      document.querySelector('#modal-room-name input').focus();
    },
    clearRoomName() {
      this.newRoomName = '';
    },
    resumeLeaveBtn() {
      document.querySelector('#room #leave-room').removeAttribute('disabled');
    },
    closeGameResult(e) {
      e.target.closest('#end-game').classList.remove('show');
    },
  },
  computed: {
    readyBtnControl() {
      return this.isPlaying ||
            this.isGuest ||
            this.playerList.length < 2;
    },
    leaveBtnControl() {
      return !this.isGuest && this.isPlaying;
    }
  },
  created: function() {
    this.connection.start()
      .then(function() {})
      .catch(function(err) {
        return console.error(err.toString());
      });
    this.connection.on('UpdateUserCount', function(count) {
      app.userCount = count;
    });
    this.connection.on('UpdateRoomList', function(roomList) {
      let list = JSON.parse(roomList);
      list.forEach((room) => {
        room.RoomStatus = roomStatus[room.RoomStatus];
      });
      app.roomList = list;
    });
    this.connection.on('UpdateReadyPlayer', function(data) {
      app.readyArray = JSON.parse(data);
    });
    this.connection.on('MyColor', function(color) {
      app.color = '';
      app.color = color;
    });
    this.connection.on('ReturnRoomId', function(roomId) {
      app.roomId = roomId;
      app.resumeLeaveBtn();
    });
    this.connection.on('ReturnIsPlaying', function(isPlaying) {
      app.isPlaying = isPlaying;
    });
    this.connection.on('ReturnIsGuest', function(isGuest) {
      app.isGuest = isGuest;
    });
    this.connection.on('ReturnPlayers', function(playerList) {
      app.playerList = JSON.parse(playerList);
    });
    this.connection.on('ReturnPlayerColor', function(data) {
      let colors = JSON.parse(data);
      let tags = document.querySelectorAll('#room .player-list .color');
      tags[0].classList.add(colors[0]);
      tags[1].classList.add(colors[1]);
    });
    this.connection.on('ReturnPieceData', function(pieceData) {
      let dataList = JSON.parse(pieceData);
      dataList.forEach((data) => {
        let piece = app.pieceArray[data[1]][data[0]];
        piece.classList.add('active');
        piece.classList.add(data[2] == 1 ? 'black' : 'white');
      });
    });
    this.connection.on('UpdateTimeLeft', function(timeLeft) {
      app.timeLeft = timeLeft;
    });
    this.connection.on('StartGame', function() {
      app.isPlaying = true;
      app.isReady = false;
      app.readyArray = [false, false];
      for (let y = 1; y <= 15; y++) {
        for (let x = 1; x <= 15; x++) {
          app.pieceArray[y][x].onclick = app.setPiece;
          app.pieceArray[y][x].classList.remove('active', 'black', 'white');
          if (app.color) app.pieceArray[y][x].classList.add(app.color);
        }
      }
    });
    this.connection.on('EndGame', function(winner) {
      let winnerText = winner === 'black' ? '黑子' : '白子';
      let endGame = app.$el.querySelector('#end-game');

      endGame.classList.add('show');
      endGame.querySelector('.winner').textContent = winnerText;
      setTimeout(() => {
        endGame.classList.remove('show');
      }, 5000);

      app.isPlaying = false;
      app.timeLeft = null;
      app.turnIndex = null;
      app.$el.querySelector('#board').classList.remove('my-turn');
    });
    this.connection.on('ReturnTurnIndex', function(turnIndex) {
      app.turnIndex = turnIndex;
    });
    this.connection.on('ControlBoard', function(isMyTurn) {
      if (isMyTurn)
        app.$el.querySelector('#board').classList.add('my-turn');
      else
        app.$el.querySelector('#board').classList.remove('my-turn');
    });
    this.connection.on('UpdateMessage', function(userName, message) {
      app.messageQueue.push({
        user: userName,
        message: message,
      });
      if (app.messageQueue.length > 40)
        app.messageQueue.shift();
    });
    this.$nextTick(() => {
      this.$el.querySelector('#form-input-name input').focus();
    });
  },
});