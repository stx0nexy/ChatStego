import { Grid, Typography} from '@mui/material';


export const ChatHeader = ({ onMenuClick, onProfileClick, onLogout, onAddContactClick, anchorEl, open, onClose, onCreateChat }) => (
  <Grid container spacing={2}>
    <Grid item xs={4}>
      
    </Grid>
    <Grid item xs={8}>
    <Typography variant="h6" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
  <span>Contacts</span>
</Typography>
      
    </Grid>
  </Grid>
);
