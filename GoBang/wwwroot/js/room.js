toastr.options = {
  'timeOut': 2000,
  'showDuration': 200,
  'hideDuration': 200,
  'preventDuplicates': true,
  'positionClass': 'toast-top-center',
};

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
    isGuest: true,
    timeLeft: 10,
    turnFlag: null,
    readyArray: [false, false],
    playerColor: [null, null],
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
      this.roomId = '';
      this.scenes = 'hall';
      this.isReady = false;
    },
    setBoard() {
      //綁定XY座標
      let arr = document.querySelectorAll('#piece-group .piece');
      for (let y = 1; y <= 15; y++) {
        for (let x = 1; x <= 15; x++) {
          this.pieceArray[y][x] = arr[(y - 1) * 15 + x - 1];
          this.pieceArray[y][x].x = x;
          this.pieceArray[y][x].y = y;
          this.pieceArray[y][x].addEventListener('click', this.setPiece);
        }
      }
      //更新雙方棋子
      this.connection.on('UpdateBoard', function(x, y, color) {
        let piece = app.pieceArray[y][x];
        piece.classList.add('active');
        piece.classList.replace(app.color, color);
        piece.removeEventListener('click', app.setPiece);
      });
    },
    piecePosition(i) {
      return {
        top: `${(Math.floor((i - 1) / 15) + 1) * 40}px`,
        left: `${(i % 15 == 0 ? 15 : i % 15) * 40}px`
      }
    },
    setPiece(e) {
      console.log(`x: ${e.target.x}`, `y: ${e.target.y}`);
      // 發送資料
      this.connection.invoke('SetPiece', this.roomId, e.target.x, e.target.y, this.color).catch(function(err) {
        return console.error(err.toString());
      });
      // 禁止點擊
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
      // this.connection.invoke('SendMessage', );
    },
    focusInput() {
      document.querySelector('#modal-room-name input').focus();
    },
    clearRoomName() {
      this.newRoomName = '';
    },
    resumeLeaveBtn() {
      document.querySelector('#room #leave-room').removeAttribute('disabled');
    }
  },
  computed: {
    readyBtnControl() {
      return this.isPlaying ||
              this.isGuest ||
              this.playerList.length < 2;
    },
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
      app.roomList = JSON.parse(roomList);
    });
    this.connection.on('UpdateReadyPlayer', function(data) {
      app.readyArray = JSON.parse(data);
    });
    this.connection.on('ReturnRoomId', function(roomId) {
      app.roomId = roomId;
      app.resumeLeaveBtn();
    });
    this.connection.on('ReturnPlayers', function(playerList) {
      app.playerList = JSON.parse(playerList);
    });
    this.connection.on('ReturnColor', function(color) {
      app.color = color;
    });
    this.connection.on('ReturnPlayerColor', function(data) {
      let colors = JSON.parse(data);
      let tags = document.querySelectorAll('#room .player-list .color');
      tags[0].classList.add(colors[0]);
      tags[1].classList.add(colors[1]);
    });
    this.connection.on('ReturnIsGuest', function(isGuest) {
      app.isGuest = isGuest;
    });
    this.connection.on('UpdateTimeLeft', function(timeLeft) {
      app.timeLeft = timeLeft;
    });
    this.connection.on('StartGame', function() {
      app.isPlaying = true;
      //TODO: 關閉雙按鈕
    });
    this.connection.on('WhosTurn', function(turnFlag) {
      app.turnFlag = turnFlag;
    });
    this.$nextTick(() => {
      this.$el.querySelector('#form-input-name input').focus();
    });
  },
});